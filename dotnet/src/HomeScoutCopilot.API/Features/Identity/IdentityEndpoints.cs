using System.Security.Claims;
using Carter;
using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Shared.Contracts;

namespace HomeScoutCopilot.API.Features.Identity;

/// <summary>
/// Identity endpoints. <c>GET /api/me</c> is the smallest end-to-end proof that a Keycloak token
/// validates and resolves to a caller — and the seam the frontend calls to render the signed-in
/// user. Requires a valid bearer token; anonymous callers get 401. Resolves the token to the
/// internal user (<see cref="MeResponse.UserId"/>) via the <see cref="IUserDirectory"/>.
/// </summary>
public sealed class IdentityEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app) =>
        app.MapGet("/api/me", async (ClaimsPrincipal user, IUserResolver resolver, CancellationToken cancellationToken) =>
            {
                // The subject is the stable identity key. A validated token without one is unusable.
                var subject = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(subject))
                {
                    return Results.Unauthorized();
                }

                var email = user.FindFirstValue("email");
                var name = user.FindFirstValue("name")
                    ?? user.FindFirstValue("preferred_username")
                    ?? user.Identity?.Name;

                // Resolve (get-or-create, cached) the internal user id; null when no database.
                var userId = await resolver.ResolveUserIdAsync(user, cancellationToken);
                return Results.Ok(new MeResponse(userId, subject, email, name));
            })
            .RequireAuthorization()
            .WithName("GetMe")
            .WithTags("Identity")
            .WithSummary("The signed-in user's identity, from the validated token")
            .Produces<MeResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
}
