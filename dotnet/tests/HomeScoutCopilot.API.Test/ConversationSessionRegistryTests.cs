using HomeScoutCopilot.API.Service;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Options;

namespace HomeScoutCopilot.API.Test;

// Offline tests for the session store's logic (reuse, remove, idle/absolute eviction) with a fake
// AgentSession — no LLM. The real multi-turn context-carry is verified live in FoundryAgentGatewayLiveTests.
[TestFixture]
public class ConversationSessionRegistryTests
{
    private sealed class FakeSession : AgentSession;

    private static ConversationSessionRegistry Registry(TimeSpan idle, TimeSpan absolute) =>
        new(Options.Create(new ConversationOptions { IdleTimeout = idle, AbsoluteLifetime = absolute }));

    private static Task<AgentSession> NewFake() => Task.FromResult<AgentSession>(new FakeSession());

    [Test]
    public async Task GetOrCreate_reuses_the_same_session_per_id()
    {
        var registry = Registry(TimeSpan.FromMinutes(60), TimeSpan.FromHours(24));
        var created = 0;
        Task<AgentSession> Factory()
        {
            created++;
            return NewFake();
        }

        var a = await registry.GetOrCreateAsync("s1", Factory);
        var b = await registry.GetOrCreateAsync("s1", Factory);
        var c = await registry.GetOrCreateAsync("s2", Factory);

        Assert.Multiple(() =>
        {
            Assert.That(b, Is.SameAs(a), "same id returns the same session");
            Assert.That(c, Is.Not.SameAs(a), "a different id returns a different session");
            Assert.That(created, Is.EqualTo(2), "the factory runs once per new id");
            Assert.That(registry.Count, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task Remove_drops_the_session()
    {
        var registry = Registry(TimeSpan.FromMinutes(60), TimeSpan.FromHours(24));
        await registry.GetOrCreateAsync("s1", NewFake);

        Assert.Multiple(() =>
        {
            Assert.That(registry.Remove("s1"), Is.True);
            Assert.That(registry.Remove("missing"), Is.False);
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task Sweep_evicts_idle_and_expired_sessions()
    {
        var registry = Registry(idle: TimeSpan.FromMinutes(30), absolute: TimeSpan.FromHours(8));
        await registry.GetOrCreateAsync("s1", NewFake);

        Assert.Multiple(() =>
        {
            Assert.That(registry.Sweep(DateTimeOffset.UtcNow), Is.EqualTo(0), "nothing expired yet");
            Assert.That(registry.Sweep(DateTimeOffset.UtcNow.AddHours(9)), Is.EqualTo(1), "idle + absolute exceeded");
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }
}
