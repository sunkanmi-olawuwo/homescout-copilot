using System.Text;
using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Shared.Contracts;

namespace HomeScoutCopilot.Evaluator;

/// <summary>
/// Drives each multi-turn case against the <em>live</em> copilot over a single session, so a terse
/// follow-up (e.g. "and on interest-only?") is answered from the earlier turn's figures. The check
/// is context-carry: the final answer must contain the expected carried-over figure — the whole
/// point of threads is that this holds without restating the numbers. Every turn of a case shares
/// one session id; different cases use different ids so they never bleed into each other.
/// </summary>
public static class MultiTurnEvaluation
{
    public static async Task<IReadOnlyList<MultiTurnResult>> RunAsync(
        IHomeScoutAgentGateway gateway,
        IEnumerable<MultiTurnCase> cases,
        CancellationToken cancellationToken = default)
    {
        var results = new List<MultiTurnResult>();
        foreach (var scenario in cases)
        {
            var sessionId = $"eval-{scenario.Id}";
            var finalAnswer = string.Empty;

            // Replay the turns in order against the same session so context accumulates.
            foreach (var turn in scenario.Turns)
            {
                var answer = await gateway
                    .AskAsync(new CopilotRequest(turn), sessionId, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                finalAnswer = answer.Caveats.Count > 0
                    ? $"{answer.Text}\n{string.Join("\n", answer.Caveats)}"
                    : answer.Text;
            }

            var carried = finalAnswer.Contains(scenario.ExpectFinalContains, StringComparison.OrdinalIgnoreCase);
            results.Add(new MultiTurnResult(scenario.Id, finalAnswer, carried));
        }

        return results;
    }

    /// <summary>True when every case's final answer carried context.</summary>
    public static bool AllCarried(IEnumerable<MultiTurnResult> results) => results.All(r => r.CarriedContext);

    public static string Summarise(IReadOnlyList<MultiTurnResult> results)
    {
        var sb = new StringBuilder();
        sb.Append("HomeScout multi-turn (context-carry) evaluation\n");
        sb.Append("===============================================\n");
        sb.Append($"Conversations: {results.Count}\n\n");

        foreach (var result in results)
        {
            sb.Append($"  {(result.CarriedContext ? "PASS" : "FAIL")}  {result.Id}\n");
        }

        var carried = results.Count(r => r.CarriedContext);
        sb.Append($"\nResult: {carried}/{results.Count} conversations carried context — ");
        sb.Append(AllCarried(results) ? "PASS\n" : "FAIL\n");
        return sb.ToString();
    }
}
