using System.Collections.Concurrent;
using System.Text.Json;
using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Shared.Contracts;

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

    public Task<IReadOnlyList<ConversationSummary>> ListForUserAsync(
        Guid userId, int limit, CancellationToken cancellationToken = default)
    {
        var owned = Owners
            .Where(kvp => kvp.Value == userId)
            .Select(kvp => new ConversationSummary(kvp.Key, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch))
            .ToList();
        return Task.FromResult<IReadOnlyList<ConversationSummary>>(owned);
    }

    public Task<ConversationSummary?> GetForUserAsync(
        string sessionId, Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult(Owners.TryGetValue(sessionId, out var owner) && owner == userId
            ? new ConversationSummary(sessionId, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch)
            : null);
}
