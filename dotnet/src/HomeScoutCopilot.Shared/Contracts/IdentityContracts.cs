namespace HomeScoutCopilot.Shared.Contracts;

/// <summary>
/// The signed-in user's identity, resolved from the validated Keycloak token. The internal user id
/// is added once the user directory lands (Keycloak auth plan step 3); for now this carries the
/// token's subject and profile claims.
/// </summary>
public record MeResponse(string Subject, string? Email, string? Name);
