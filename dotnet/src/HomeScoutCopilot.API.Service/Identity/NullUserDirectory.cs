namespace HomeScoutCopilot.API.Service;

/// <summary>
/// No-op <see cref="IUserDirectory"/> — the graceful default when no database is configured. Tokens
/// still validate and anonymous + authenticated requests still work; users simply aren't persisted,
/// so per-user history is unavailable. Mirrors <see cref="NullSessionStore"/>.
/// </summary>
public sealed class NullUserDirectory : IUserDirectory
{
    public bool IsEnabled => false;

    public Task<UserRecord?> RecordAsync(
        string provider, string subject, string? email, string? name, CancellationToken cancellationToken = default)
        => Task.FromResult<UserRecord?>(null);
}
