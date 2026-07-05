using Carter;
using HomeScoutCopilot.Shared.Contracts;

namespace HomeScoutCopilot.API.Features.Config;

/// <summary>
/// Public client configuration. <c>GET /api/config</c> tells the SPA how to sign in (the Keycloak
/// authority + client id + API audience) without hardcoding the Aspire-assigned Keycloak URL. Un-
/// authenticated by design — it carries no secrets, only what a public OIDC client already needs.
/// </summary>
public sealed class ConfigEndpoints : ICarterModule
{
    // The realm/client/audience match the committed realm export + the API's token validation.
    private const string Realm = "homescout";
    private const string ClientId = "homescout-web";
    private const string Audience = "homescout-api";

    public void AddRoutes(IEndpointRouteBuilder app) =>
        app.MapGet("/api/config", (IConfiguration configuration) =>
            {
                // Aspire injects the Keycloak endpoint as service-discovery config; prefer https.
                var keycloakUrl = configuration["services:keycloak:https:0"]
                    ?? configuration["services:keycloak:http:0"];
                var authority = string.IsNullOrWhiteSpace(keycloakUrl)
                    ? null
                    : $"{keycloakUrl.TrimEnd('/')}/realms/{Realm}";

                return Results.Ok(new AuthConfigResponse(authority is not null, authority, ClientId, Audience));
            })
            .WithName("GetClientConfig")
            .WithTags("Config")
            .WithSummary("Public client config for OIDC sign-in (authority, client id, audience)")
            .Produces<AuthConfigResponse>();
}
