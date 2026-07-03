using HomeScoutCopilot.Shared.Application.Contracts;

namespace HomeScoutCopilot.API.Service;

/// <summary>
/// The copilot's orchestration boundary. Turns a natural-language request into tool
/// calls + a grounded answer. The Foundry-backed implementation (Microsoft Agent
/// Framework) arrives in a later slice; this interface keeps the agent behind the API.
/// </summary>
public interface IHomeScoutAgentGateway
{
    Task<CopilotAnswer> AskAsync(CopilotRequest request, CancellationToken cancellationToken = default);
}
