using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HomeScoutCopilot.API.Service;

/// <summary>
/// Periodically evicts idle/expired anonymous conversation sessions from the
/// <see cref="ConversationSessionRegistry"/>, so in-memory session state stays bounded.
/// </summary>
public sealed class ConversationSessionSweeper(
    ConversationSessionRegistry registry,
    IOptions<ConversationOptions> options,
    ILogger<ConversationSessionSweeper> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.SweepInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            var evicted = registry.Sweep(DateTimeOffset.UtcNow);
            if (evicted > 0)
            {
                logger.LogDebug("Evicted {Count} idle/expired conversation session(s).", evicted);
            }
        }
    }
}
