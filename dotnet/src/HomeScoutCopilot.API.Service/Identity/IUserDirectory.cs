namespace HomeScoutCopilot.API.Service;

/// <summary>Identity providers HomeScout resolves. End-user sign-in is Keycloak (see Plan Divergence).</summary>
public static class UserIdentityProviders
{
    public const string Keycloak = "keycloak";
}

/// <summary>An internal HomeScout user, keyed by its canonical <see cref="Id"/> (a uuid we own) —
/// never the raw OIDC subject, so the identity provider stays swappable.</summary>
public sealed record UserRecord(Guid Id, string Provider, string Subject, string? Email, string? Name);

/// <summary>
/// Resolves an external OIDC identity <c>(provider, subject)</c> to an internal
/// <see cref="UserRecord"/>, creating it on first sign-in (get-or-create). The in-memory analogue
/// of <see cref="ISessionStore"/>: <see cref="PostgresUserDirectory"/> is the real adapter and
/// <see cref="NullUserDirectory"/> the graceful "no database" default (auth still works; users just
/// aren't persisted, so per-user history is unavailable).
/// </summary>
public interface IUserDirectory
{
    /// <summary>True when users are actually persisted (a database is configured).</summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Records a sighting of <paramref name="subject"/> from <paramref name="provider"/>, returning
    /// the canonical user. Idempotent and race-safe: concurrent first sign-ins converge on one row.
    /// Returns null when the directory is disabled (no database).
    /// </summary>
    Task<UserRecord?> RecordAsync(
        string provider, string subject, string? email, string? name, CancellationToken cancellationToken = default);
}
