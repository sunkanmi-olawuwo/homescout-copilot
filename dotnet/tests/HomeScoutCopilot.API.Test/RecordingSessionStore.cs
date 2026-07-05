using System.Collections.Concurrent;
using System.Text.Json;
using HomeScoutCopilot.API.Service;

namespace HomeScoutCopilot.API.Test;

// Test double: an in-memory ISessionStore that records the calls made to it, so wiring tests
// (reset endpoint, sweeper) can assert the durable store was invoked without a real database.
internal sealed class RecordingSessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<string, JsonElement> _saved = new();

    public bool IsPersistent { get; init; } = true;
    public ConcurrentBag<string> Removed { get; } = [];

    public Task<JsonElement?> TryLoadAsync(string sessionId, CancellationToken cancellationToken = default)
        => Task.FromResult(_saved.TryGetValue(sessionId, out var payload) ? payload : (JsonElement?)null);

    public ConcurrentDictionary<string, Guid?> Owners { get; } = new();

    public Task SaveAsync(string sessionId, JsonElement payload, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        _saved[sessionId] = payload.Clone();
        Owners[sessionId] = userId;
        return Task.CompletedTask;
    }

    public Task<bool> RemoveAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        Removed.Add(sessionId);
        return Task.FromResult(_saved.TryRemove(sessionId, out _));
    }

    public Task<int> SweepExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
        => Task.FromResult(0);
}
