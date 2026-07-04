using System.Text.Json;
using HomeScoutCopilot.API.Service;

namespace HomeScoutCopilot.API.Test;

// The no-op store is the "durability off" default (no DB configured). It must never persist or
// claim to — the gateway relies on IsPersistent=false to skip serializing sessions.
[TestFixture]
public class NullSessionStoreTests
{
    private readonly NullSessionStore _store = new();

    [Test]
    public void Is_not_persistent()
        => Assert.That(_store.IsPersistent, Is.False);

    [Test]
    public async Task Load_after_save_still_returns_null()
    {
        using var doc = JsonDocument.Parse("""{"state":1}""");
        await _store.SaveAsync("s1", doc.RootElement);

        Assert.That(await _store.TryLoadAsync("s1"), Is.Null);
    }

    [Test]
    public async Task Remove_reports_nothing_removed_and_sweep_evicts_nothing()
    {
        Assert.That(await _store.RemoveAsync("s1"), Is.False);
        Assert.That(await _store.SweepExpiredAsync(DateTimeOffset.UtcNow), Is.EqualTo(0));
    }
}
