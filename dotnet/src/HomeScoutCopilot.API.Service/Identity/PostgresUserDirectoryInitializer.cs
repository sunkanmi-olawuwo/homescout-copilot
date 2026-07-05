using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HomeScoutCopilot.API.Service;

/// <summary>
/// Ensures the <see cref="PostgresUserDirectory"/> schema exists before the app serves traffic.
/// Registered only when PostgreSQL is configured (mirrors <see cref="PostgresSessionStoreInitializer"/>).
/// </summary>
public sealed class PostgresUserDirectoryInitializer(
    PostgresUserDirectory directory,
    ILogger<PostgresUserDirectoryInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await directory.InitializeAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("User directory (PostgreSQL) is ready.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
