using Carter;
using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Shared.Contracts;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HomeScoutCopilot.API.Features.Copilot;

public sealed class CopilotEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/copilot/ask",
                async (HttpContext http, IMediator mediator, IUserResolver resolver, CopilotRequest request,
                    IOptions<ConversationOptions> conversation) =>
                {
                    // Anonymous-capable: the session id always exists (cookie); the owner is resolved
                    // only when the request carries a valid token (null otherwise).
                    var sessionId = ResolveSession(http, conversation.Value);
                    var userId = await resolver.ResolveUserIdAsync(http.User, http.RequestAborted);
                    return await mediator.Send(new AskCopilotCommand(request, sessionId, userId));
                })
            .WithName("AskCopilot")
            .WithTags("Copilot")
            .WithSummary("Ask the copilot a natural-language question")
            .Produces<CopilotAnswer>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        // Start a fresh conversation: drop the current session from memory + the durable store and
        // clear the cookie.
        app.MapPost("/api/copilot/session/reset",
                async (HttpContext http, ConversationSessionRegistry sessions, ISessionStore store,
                    IOptions<ConversationOptions> conversation) =>
                {
                    if (http.Request.Cookies.TryGetValue(conversation.Value.CookieName, out var id) && !string.IsNullOrEmpty(id))
                    {
                        sessions.Remove(id);
                        await store.RemoveAsync(id, http.RequestAborted);
                    }

                    http.Response.Cookies.Delete(conversation.Value.CookieName);
                    return Results.NoContent();
                })
            .WithName("ResetCopilotSession")
            .WithTags("Copilot")
            .WithSummary("Start a new conversation (clear the anonymous session)")
            .Produces(StatusCodes.Status204NoContent);
    }

    // Reads the anonymous session cookie, issuing a fresh HttpOnly one on first contact. The id is
    // opaque and server-issued; the browser sends it automatically, so the frontend needs no code.
    private static string ResolveSession(HttpContext http, ConversationOptions options)
    {
        if (http.Request.Cookies.TryGetValue(options.CookieName, out var existing) && !string.IsNullOrEmpty(existing))
        {
            return existing;
        }

        var sessionId = Guid.NewGuid().ToString("N");
        http.Response.Cookies.Append(options.CookieName, sessionId, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = http.Request.IsHttps,
            MaxAge = options.AbsoluteLifetime,
            Path = "/",
        });
        return sessionId;
    }
}

public sealed record AskCopilotCommand(CopilotRequest Request, string SessionId, Guid? UserId) : IRequest<IResult>;

// Resolves the gateway from the request scope so the endpoint returns 503 (not a DI
// failure) when the copilot isn't configured — preserving the pre-slice behaviour.
public sealed class AskCopilotHandler(IServiceProvider services) : IRequestHandler<AskCopilotCommand, IResult>
{
    public async Task<IResult> Handle(AskCopilotCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Request.Message))
        {
            return Results.Problem(title: "A message is required.", statusCode: StatusCodes.Status400BadRequest);
        }

        var gateway = services.GetService<IHomeScoutAgentGateway>();
        if (gateway is null)
        {
            return Results.Problem(
                title: "Copilot is not configured",
                detail: "The Foundry project endpoint is not set. Provision Foundry (azd) and set Foundry:ProjectEndpoint / AZURE_FOUNDRY_PROJECT_ENDPOINT.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Results.Ok(await gateway.AskAsync(command.Request, command.SessionId, command.UserId, cancellationToken));
    }
}
