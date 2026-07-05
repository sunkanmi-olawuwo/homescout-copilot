using HomeScoutCopilot.API.Service;

namespace HomeScoutCopilot.API.Test;

// The no-op directory is the "no database" default: auth still works but users aren't persisted.
[TestFixture]
public class NullUserDirectoryTests
{
    private readonly NullUserDirectory _directory = new();

    [Test]
    public void Is_not_enabled()
        => Assert.That(_directory.IsEnabled, Is.False);

    [Test]
    public async Task Record_returns_null()
        => Assert.That(
            await _directory.RecordAsync(UserIdentityProviders.Keycloak, "sub-1", "a@b.com", "A B"),
            Is.Null);
}
