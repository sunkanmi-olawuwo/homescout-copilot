using System.Security.Claims;
using Carter;
using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Shared.Contracts;

namespace HomeScoutCopilot.API.Features.Copilot;

/// <summary>
/// Per-user conversation history. Authorized (a valid bearer token) and strictly owner-scoped — the
/// store's queries filter by the caller's internal user id, so a user can never read another's
/// conversations. Resolves the caller via <see cref="IUserResolver"/>; when there's no database the
/// history is simply empty.
/// </summary>
public sealed class HistoryEndpoints : ICarterModule
{
    private const int MaxConversations = 50;

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/copilot/history",
                async (ClaimsPrincipal user, IUserResolver resolver, ISessionStore store, CancellationToken cancellationToken) =>
                {
                    var userId = await resolver.ResolveUserIdAsync(user, cancellationToken);
                    if (userId is null)
                    {
                        return Results.Ok(new ConversationHistoryResponse([]));
                    }

                    var conversations = await store.ListForUserAsync(userId.Value, MaxConversations, cancellationToken);
                    return Results.Ok(new ConversationHistoryResponse(conversations));
                })
            .RequireAuthorization()
            .WithName("GetCopilotHistory")
            .WithTags("Copilot")
            .WithSummary("The signed-in user's past conversations, most-recent-first")
            .Produces<ConversationHistoryResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        app.MapGet("/api/copilot/history/{sessionId}",
                async (string sessionId, ClaimsPrincipal user, IUserResolver resolver, ISessionStore store,
                    CancellationToken cancellationToken) =>
                {
                    var userId = await resolver.ResolveUserIdAsync(user, cancellationToken);
                    if (userId is null)
                    {
                        return Results.NotFound();
                    }

                    // 404 (not 403) when not owned — don't reveal that the session exists for someone else.
                    var summary = await store.GetForUserAsync(sessionId, userId.Value, cancellationToken);
                    return summary is null ? Results.NotFound() : Results.Ok(summary);
                })
            .RequireAuthorization()
            .WithName("GetCopilotConversation")
            .WithTags("Copilot")
            .WithSummary("A specific owned conversation (404 if not the owner)")
            .Produces<ConversationSummary>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
