using HomeScoutCopilot.API.Service;

namespace HomeScoutCopilot.API.Test;

// Enabled test directory that resolves any subject to one fixed internal id (no database) — for
// endpoint tests that need an authenticated caller to map to a known internal user.
internal sealed class FakeUserDirectory : IUserDirectory
{
    public static readonly Guid FixedUserId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");

    public bool IsEnabled => true;

    public Task<UserRecord?> RecordAsync(
        string provider, string subject, string? email, string? name, CancellationToken cancellationToken = default)
        => Task.FromResult<UserRecord?>(new UserRecord(FixedUserId, provider, subject, email, name));
}
