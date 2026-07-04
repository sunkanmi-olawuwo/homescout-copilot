using System.Collections.Concurrent;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Options;

namespace HomeScoutCopilot.API.Service;

/// <summary>
/// In-memory store of per-session <see cref="AgentSession"/>s (the agent's multi-turn conversation
/// state), keyed by an anonymous session id. Singleton — the session outlives the request-scoped
/// gateway/agent, so follow-up turns keep context. Sliding idle + absolute cap are enforced by
/// <see cref="Sweep"/> (driven by the background sweeper). A durable (Cosmos) store can replace this
/// later via the agent's session serialization; the shape stays the same.
/// </summary>
public sealed class ConversationSessionRegistry(IOptions<ConversationOptions> options)
{
    private readonly ConversationOptions _options = options.Value;
    private readonly ConcurrentDictionary<string, Entry> _sessions = new();

    public int Count => _sessions.Count;

    /// <summary>Returns the session for <paramref name="sessionId"/>, creating it via
    /// <paramref name="factory"/> on first use. Refreshes the idle timer.</summary>
    public async Task<AgentSession> GetOrCreateAsync(string sessionId, Func<Task<AgentSession>> factory)
    {
        if (_sessions.TryGetValue(sessionId, out var existing))
        {
            existing.Touch();
            return existing.Session;
        }

        // Create outside the dictionary, then add. If a concurrent request won the race, use theirs
        // (this session is simply discarded) — a conversation is sequential, so this is rare.
        var session = await factory().ConfigureAwait(false);
        var entry = _sessions.GetOrAdd(sessionId, new Entry(session));
        entry.Touch();
        return entry.Session;
    }

    /// <summary>Removes a session (used by conversation reset).</summary>
    public bool Remove(string sessionId) => _sessions.TryRemove(sessionId, out _);

    /// <summary>Evicts sessions idle past <see cref="ConversationOptions.IdleTimeout"/> or older than
    /// <see cref="ConversationOptions.AbsoluteLifetime"/>. Returns the number evicted.</summary>
    public int Sweep(DateTimeOffset now)
    {
        var evicted = 0;
        foreach (var (id, entry) in _sessions)
        {
            if (now - entry.LastAccessUtc > _options.IdleTimeout || now - entry.CreatedUtc > _options.AbsoluteLifetime)
            {
                if (_sessions.TryRemove(id, out _))
                {
                    evicted++;
                }
            }
        }

        return evicted;
    }

    private sealed class Entry(AgentSession session)
    {
        public AgentSession Session { get; } = session;
        public DateTimeOffset CreatedUtc { get; } = DateTimeOffset.UtcNow;
        public DateTimeOffset LastAccessUtc { get; private set; } = DateTimeOffset.UtcNow;

        public void Touch() => LastAccessUtc = DateTimeOffset.UtcNow;
    }
}
