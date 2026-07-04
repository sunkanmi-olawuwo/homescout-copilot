using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Shared.Contracts;

namespace HomeScoutCopilot.Evaluator.Test;

// Offline test double for the agent gateway that records every call (message + session id), so the
// multi-turn harness tests can assert the turns were driven in order against one session — without
// a live model. The responder maps a turn message to the answer the "copilot" returns.
internal sealed class RecordingGateway(Func<string, CopilotAnswer> responder) : IHomeScoutAgentGateway
{
    public List<(string Message, string? SessionId)> Calls { get; } = [];

    public Task<CopilotAnswer> AskAsync(
        CopilotRequest request, string? sessionId = null, CancellationToken cancellationToken = default)
    {
        Calls.Add((request.Message, sessionId));
        return Task.FromResult(responder(request.Message));
    }

    public static CopilotAnswer Answer(string text) => new(text, [], [], [], []);
}
