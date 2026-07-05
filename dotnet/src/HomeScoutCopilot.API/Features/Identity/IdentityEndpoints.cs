using System.Security.Claims;
using Carter;
using HomeScoutCopilot.Shared.Contracts;

namespace HomeScoutCopilot.API.Features.Identity;

/// <summary>
/// Identity endpoints. <c>GET /api/me</c> is the smallest end-to-end proof that a Keycloak token
/// validates and resolves to a caller — and the seam the frontend calls to render the signed-in
/// user. Requires a valid bearer token; anonymous callers get 401.
/// </summary>
public sealed class IdentityEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app) =>
        app.MapGet("/api/me", (ClaimsPrincipal user) =>
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

                return Results.Ok(new MeResponse(subject, email, name));
            })
            .RequireAuthorization()
            .WithName("GetMe")
            .WithTags("Identity")
            .WithSummary("The signed-in user's identity, from the validated token")
            .Produces<MeResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
}
