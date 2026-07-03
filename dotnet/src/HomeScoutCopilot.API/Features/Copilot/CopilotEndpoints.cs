using Carter;
using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Shared.Contracts;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace HomeScoutCopilot.API.Features.Copilot;

public sealed class CopilotEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app) =>
        app.MapPost("/api/copilot/ask", (IMediator mediator, CopilotRequest request)
                => mediator.Send(new AskCopilotCommand(request)))
            .WithName("AskCopilot")
            .WithTags("Copilot")
            .WithSummary("Ask the copilot a natural-language question")
            .Produces<CopilotAnswer>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
}

public sealed record AskCopilotCommand(CopilotRequest Request) : IRequest<IResult>;

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

        return Results.Ok(await gateway.AskAsync(command.Request, cancellationToken));
    }
}
