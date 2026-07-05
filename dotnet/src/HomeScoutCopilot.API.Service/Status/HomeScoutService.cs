using FluentResults;
using HomeScoutCopilot.Shared.Contracts;

namespace HomeScoutCopilot.API.Service;

/// <summary>
/// Application-layer boundary for HomeScout behaviour. Returns FluentResults so
/// expected failures are values, not exceptions. Future agent-gateway and tool
/// orchestration lands behind this interface.
/// </summary>
public interface IHomeScoutService
{
    Result<HomeScoutStatus> GetStatus();
}

public sealed class HomeScoutService : IHomeScoutService
{
    public Result<HomeScoutStatus> GetStatus() =>
        Result.Ok(new HomeScoutStatus(
            Product: "HomeScout Copilot",
            Frontend: "React",
            Architecture: "API-first",
            AgentPlatform: "Microsoft Foundry Agent Service planned"));
}
