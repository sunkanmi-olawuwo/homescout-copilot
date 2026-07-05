using System.Text.Json;
using HomeScoutCopilot.Shared.Contracts;

namespace HomeScoutCopilot.API.Service;

/// <summary>
/// Durable persistence for anonymous conversation sessions — the agent's serialized multi-turn
/// state (the <see cref="JsonElement"/> produced by <c>AIAgent.SerializeSessionAsync</c>), keyed by
/// the opaque session id. The in-memory <see cref="ConversationSessionRegistry"/> stays the hot
/// cache; this store is what lets history survive an API restart.
/// </summary>
/// <remarks>
/// Two shipped implementations: <see cref="PostgresSessionStore"/> (the real adapter) and
/// <see cref="NullSessionStore"/> (a no-op used when no database is configured, so sessions simply
/// live in memory as before — graceful degradation, not a test double).
/// </remarks>
public interface ISessionStore
{
    /// <summary>
    /// True when this store actually persists. Lets callers skip the cost of serializing a session
    /// they would only hand to a no-op store.
    /// </summary>
    bool IsPersistent { get; }

    /// <summary>Loads the serialized session state for <paramref name="sessionId"/>, or null if absent.</summary>
    Task<JsonElement?> TryLoadAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts the serialized session state, refreshing its last-active timestamp. When
    /// <paramref name="userId"/> is non-null the session is stamped with its owner (per-user history);
    /// a null owner never clears an already-stamped session (supports the anonymous→authenticated
    /// hand-off).
    /// </summary>
    Task SaveAsync(string sessionId, JsonElement payload, Guid? userId = null, CancellationToken cancellationToken = default);

    /// <summary>Removes a session (conversation reset). Returns true if a row was deleted.</summary>
    Task<bool> RemoveAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes sessions idle past <see cref="ConversationOptions.IdleTimeout"/> or older than
    /// <see cref="ConversationOptions.AbsoluteLifetime"/>. Returns the number deleted.
    /// </summary>
    Task<int> SweepExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>
    /// The user's own conversations, most-recent-first, capped at <paramref name="limit"/> — the
    /// per-user history list. Owner-scoped; never returns another user's sessions.
    /// </summary>
    Task<IReadOnlyList<ConversationSummary>> ListForUserAsync(
        Guid userId, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// A specific conversation <em>if owned by</em> <paramref name="userId"/>, else null (so the
    /// endpoint returns 404 without leaking whether the session exists for another user).
    /// </summary>
    Task<ConversationSummary?> GetForUserAsync(
        string sessionId, Guid userId, CancellationToken cancellationToken = default);
}
