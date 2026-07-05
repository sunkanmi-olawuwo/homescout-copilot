using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Shared.Contracts;

namespace HomeScoutCopilot.API.Test;

// Offline test double for the agent gateway — no LLM, no Azure. Returns a configured
// answer (default: a helpful prompt for the figures). The real tool-calling behaviour
// is covered by HomeScoutAgentToolsTests; the Foundry-backed gateway arrives in Slice 3.
internal sealed class FakeHomeScoutAgentGateway(Func<CopilotRequest, CopilotAnswer>? responder = null)
    : IHomeScoutAgentGateway
{
    private static readonly CopilotAnswer Default = new(
        "Tell me the property price, deposit, interest rate, and term and I'll estimate the monthly cost.",
        [],
        [],
        [],
        ["This is an estimate, not mortgage advice — speak to a qualified mortgage adviser."]);

    private readonly Func<CopilotRequest, CopilotAnswer> _responder = responder ?? (_ => Default);

    public Task<CopilotAnswer> AskAsync(
        CopilotRequest request, string? sessionId = null, Guid? userId = null, CancellationToken cancellationToken = default)
        => Task.FromResult(_responder(request));
}
