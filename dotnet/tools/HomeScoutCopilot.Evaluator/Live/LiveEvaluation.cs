using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Shared.Contracts;

namespace HomeScoutCopilot.Evaluator;

/// <summary>
/// Turns the eval dataset's <em>queries</em> into cases carrying the <em>live</em> copilot's real
/// answers — so the safety evaluators run against what the agent actually says, not authored
/// examples. The user-facing answer is the model prose plus its structured caveats (the
/// disclaimer lives there), which is what the buyer reads.
/// </summary>
public static class LiveEvaluation
{
    public static async Task<IReadOnlyList<EvaluationCase>> GenerateAsync(
        IHomeScoutAgentGateway gateway,
        IEnumerable<EvaluationCase> cases,
        CancellationToken cancellationToken = default)
    {
        var live = new List<EvaluationCase>();
        foreach (var scenario in cases)
        {
            // Each eval case is an independent single-turn ask (no session).
            var answer = await gateway.AskAsync(new CopilotRequest(scenario.Query), cancellationToken: cancellationToken);
            var response = answer.Caveats.Count > 0
                ? $"{answer.Text}\n{string.Join("\n", answer.Caveats)}"
                : answer.Text;
            live.Add(scenario with { Response = response });
        }

        return live;
    }
}
