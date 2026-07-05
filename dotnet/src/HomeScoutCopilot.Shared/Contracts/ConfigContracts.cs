namespace HomeScoutCopilot.Shared.Contracts;

/// <summary>
/// Public (unauthenticated) client configuration the SPA reads at startup to set up OIDC sign-in,
/// so the Keycloak URL isn't hardcoded in the frontend. <see cref="AuthEnabled"/> is false (and
/// <see cref="Authority"/> null) when no Keycloak is configured — the app then runs anonymous-only.
/// </summary>
public record AuthConfigResponse(bool AuthEnabled, string? Authority, string ClientId, string Audience);
