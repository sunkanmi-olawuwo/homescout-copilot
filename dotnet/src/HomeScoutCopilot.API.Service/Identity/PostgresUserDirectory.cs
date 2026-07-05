using Npgsql;

namespace HomeScoutCopilot.API.Service;

/// <summary>
/// PostgreSQL-backed <see cref="IUserDirectory"/> — resolves an external OIDC <c>(provider, subject)</c>
/// to an internal user row, race-safe on first sign-in via a single atomic upsert. Raw Npgsql for
/// consistency with <see cref="PostgresSessionStore"/> (one small table, no EF Core): the
/// <c>INSERT … ON CONFLICT (provider, subject) DO UPDATE … RETURNING</c> converges concurrent first
/// sign-ins on one row without the catch/reload/retry an EF approach needs.
/// </summary>
public sealed class PostgresUserDirectory(NpgsqlDataSource dataSource) : IUserDirectory
{
    private const string TableName = "app_users";

    public bool IsEnabled => true;

    /// <summary>Creates the users table if it does not exist. Idempotent; run once at startup.</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var command = dataSource.CreateCommand($"""
            CREATE TABLE IF NOT EXISTS {TableName} (
                id            uuid        PRIMARY KEY,
                provider      text        NOT NULL,
                subject       text        NOT NULL,
                email         text        NULL,
                name          text        NULL,
                first_seen_at timestamptz NOT NULL,
                last_seen_at  timestamptz NOT NULL,
                UNIQUE (provider, subject)
            );
            """);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<UserRecord?> RecordAsync(
        string provider, string subject, string? email, string? name, CancellationToken cancellationToken = default)
    {
        // Atomic get-or-create: on conflict the existing row is updated (bump last_seen, fill in any
        // newly-known email/name) and its id is returned — so concurrent first sign-ins all resolve
        // to the same canonical id, and a fresh @id is only used when this call wins the insert.
        await using var command = dataSource.CreateCommand($"""
            INSERT INTO {TableName} (id, provider, subject, email, name, first_seen_at, last_seen_at)
            VALUES (@id, @provider, @subject, @email, @name, @now, @now)
            ON CONFLICT (provider, subject) DO UPDATE
                SET last_seen_at = @now,
                    email = COALESCE(EXCLUDED.email, {TableName}.email),
                    name  = COALESCE(EXCLUDED.name,  {TableName}.name)
            RETURNING id, provider, subject, email, name;
            """);
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("provider", provider);
        command.Parameters.AddWithValue("subject", subject);
        command.Parameters.AddWithValue("email", (object?)email ?? DBNull.Value);
        command.Parameters.AddWithValue("name", (object?)name ?? DBNull.Value);
        command.Parameters.AddWithValue("now", DateTimeOffset.UtcNow);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        return new UserRecord(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4));
    }
}
