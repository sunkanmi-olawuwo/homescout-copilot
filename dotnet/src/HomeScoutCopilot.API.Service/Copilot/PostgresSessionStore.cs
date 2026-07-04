using System.Text.Json;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace HomeScoutCopilot.API.Service;

/// <summary>
/// PostgreSQL-backed <see cref="ISessionStore"/> — persists each anonymous session's serialized
/// agent state as a <c>jsonb</c> blob keyed by session id, so multi-turn history survives an API
/// restart. Chosen over Cosmos: the shape is a single blob-by-id table (no document model needed),
/// Aspire has first-class Postgres support, and Keycloak (later, per-user history) already runs on
/// Postgres — one engine. TTL is enforced by <see cref="SweepExpiredAsync"/>, driven by the same
/// <see cref="ConversationSessionSweeper"/> that bounds the in-memory registry.
/// </summary>
public sealed class PostgresSessionStore(NpgsqlDataSource dataSource, IOptions<ConversationOptions> options)
    : ISessionStore
{
    private const string TableName = "conversation_sessions";
    private readonly ConversationOptions _options = options.Value;

    public bool IsPersistent => true;

    /// <summary>Creates the sessions table if it does not exist. Idempotent; run once at startup.</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var command = dataSource.CreateCommand($"""
            CREATE TABLE IF NOT EXISTS {TableName} (
                session_id     text        PRIMARY KEY,
                payload        jsonb       NOT NULL,
                created_at     timestamptz NOT NULL,
                last_active_at timestamptz NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_{TableName}_last_active_at ON {TableName} (last_active_at);
            """);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<JsonElement?> TryLoadAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await using var command = dataSource.CreateCommand(
            $"SELECT payload FROM {TableName} WHERE session_id = @id");
        command.Parameters.AddWithValue("id", sessionId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var raw = reader.GetString(0);
        // Clone detaches the element from the parsed document so it outlives this scope.
        using var document = JsonDocument.Parse(raw);
        return document.RootElement.Clone();
    }

    public async Task SaveAsync(string sessionId, JsonElement payload, CancellationToken cancellationToken = default)
    {
        // Upsert: keep the original created_at on conflict (so the absolute-lifetime cap is honoured),
        // refresh last_active_at and the payload each turn (write-through).
        await using var command = dataSource.CreateCommand($"""
            INSERT INTO {TableName} (session_id, payload, created_at, last_active_at)
            VALUES (@id, @payload, @now, @now)
            ON CONFLICT (session_id) DO UPDATE
                SET payload = EXCLUDED.payload,
                    last_active_at = EXCLUDED.last_active_at
            """);
        command.Parameters.AddWithValue("id", sessionId);
        command.Parameters.Add(new NpgsqlParameter("payload", NpgsqlDbType.Jsonb) { Value = payload.GetRawText() });
        command.Parameters.AddWithValue("now", DateTimeOffset.UtcNow);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> RemoveAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await using var command = dataSource.CreateCommand(
            $"DELETE FROM {TableName} WHERE session_id = @id");
        command.Parameters.AddWithValue("id", sessionId);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return rows > 0;
    }

    public async Task<int> SweepExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        await using var command = dataSource.CreateCommand($"""
            DELETE FROM {TableName}
            WHERE last_active_at < @idleCutoff
               OR created_at < @absoluteCutoff
            """);
        command.Parameters.AddWithValue("idleCutoff", now - _options.IdleTimeout);
        command.Parameters.AddWithValue("absoluteCutoff", now - _options.AbsoluteLifetime);
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
