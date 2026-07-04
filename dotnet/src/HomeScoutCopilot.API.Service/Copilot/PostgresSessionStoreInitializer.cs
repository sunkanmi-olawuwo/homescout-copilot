using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HomeScoutCopilot.API.Service;

/// <summary>
/// Ensures the <see cref="PostgresSessionStore"/> schema exists before the app serves traffic.
/// Registered only when PostgreSQL is configured; a startup migration for a single table (explicit
/// beats lazy create-on-first-use, which would race under concurrent first turns).
/// </summary>
public sealed class PostgresSessionStoreInitializer(
    PostgresSessionStore store,
    ILogger<PostgresSessionStoreInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await store.InitializeAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Durable conversation-session store (PostgreSQL) is ready.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
