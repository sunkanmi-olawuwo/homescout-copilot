using HomeScoutCopilot.Shared.Contracts;

namespace HomeScoutCopilot.API.Service;

/// <summary>
/// Supplies the current Bank of England base rate as orienting context. Implementations
/// must <b>never throw</b>: on any failure they return a clearly-marked fallback value,
/// because the base rate is context-only and nothing on the critical path depends on it.
/// </summary>
public interface IBaseRateProvider
{
    Task<BaseRate> GetCurrentAsync(CancellationToken cancellationToken = default);
}
