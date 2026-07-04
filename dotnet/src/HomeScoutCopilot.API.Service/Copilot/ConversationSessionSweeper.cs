using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HomeScoutCopilot.API.Service;

/// <summary>
/// Periodically evicts idle/expired anonymous conversation sessions from both the in-memory
/// <see cref="ConversationSessionRegistry"/> and the durable <see cref="ISessionStore"/>, so
/// neither the process memory nor the database hoards stale session state.
/// </summary>
public sealed class ConversationSessionSweeper(
    ConversationSessionRegistry registry,
    ISessionStore store,
    IOptions<ConversationOptions> options,
    ILogger<ConversationSessionSweeper> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.SweepInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            var now = DateTimeOffset.UtcNow;
            var evicted = registry.Sweep(now);
            if (evicted > 0)
            {
                logger.LogDebug("Evicted {Count} idle/expired conversation session(s) from memory.", evicted);
            }

            // Also evict from the durable store (no-op when durability is off).
            try
            {
                var deleted = await store.SweepExpiredAsync(now, stoppingToken).ConfigureAwait(false);
                if (deleted > 0)
                {
                    logger.LogDebug("Deleted {Count} idle/expired conversation session(s) from the durable store.", deleted);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A transient DB blip must not kill the sweeper; the in-memory sweep already ran and
                // the next tick retries. Surface it without tearing down the background service.
                logger.LogWarning(ex, "Durable session-store sweep failed; will retry next tick.");
            }
        }
    }
}
