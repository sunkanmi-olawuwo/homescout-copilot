using System.Text.Json;
using HomeScoutCopilot.Shared.Contracts;

namespace HomeScoutCopilot.API.Service;

/// <summary>
/// No-op <see cref="ISessionStore"/> — the graceful-degradation default when no PostgreSQL
/// connection is configured. Sessions then live only in the in-memory
/// <see cref="ConversationSessionRegistry"/> (cleared on restart), exactly the pre-durable
/// behaviour. This is a legitimate "durability off" implementation, not a test double.
/// </summary>
public sealed class NullSessionStore : ISessionStore
{
    public bool IsPersistent => false;

    public Task<JsonElement?> TryLoadAsync(string sessionId, CancellationToken cancellationToken = default)
        => Task.FromResult<JsonElement?>(null);

    public Task SaveAsync(string sessionId, JsonElement payload, Guid? userId = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<bool> RemoveAsync(string sessionId, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<int> SweepExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
        => Task.FromResult(0);

    public Task<IReadOnlyList<ConversationSummary>> ListForUserAsync(
        Guid userId, int limit, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ConversationSummary>>([]);

    public Task<ConversationSummary?> GetForUserAsync(
        string sessionId, Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult<ConversationSummary?>(null);
}
