using HomeScoutCopilot.Shared.Contracts;

namespace HomeScoutCopilot.API.Service;

/// <summary>
/// The copilot's orchestration boundary. Turns a natural-language request into tool
/// calls + a grounded answer. The Foundry-backed implementation (Microsoft Agent
/// Framework) arrives in a later slice; this interface keeps the agent behind the API.
/// </summary>
public interface IHomeScoutAgentGateway
{
    /// <summary>
    /// Answers a request. When <paramref name="sessionId"/> is supplied, the turn runs against that
    /// session's multi-turn conversation state (follow-ups keep context); when null, it's a
    /// stateless single-turn answer. When <paramref name="userId"/> is supplied (the request is
    /// authenticated), the persisted session is stamped with that owner for per-user history.
    /// </summary>
    Task<CopilotAnswer> AskAsync(
        CopilotRequest request, string? sessionId = null, Guid? userId = null, CancellationToken cancellationToken = default);
}
