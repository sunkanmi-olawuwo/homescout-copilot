using System.Text;

namespace HomeScoutCopilot.Evaluator;

/// <summary>Aggregates model-graded quality scores into averages, a pass rate, and a summary.</summary>
public static class QualityReport
{
    public static bool AllPassed(IReadOnlyList<(string Id, JudgeScore? Score)> results) =>
        results.Count > 0 && results.All(r => r.Score is not null && r.Score.Passed());

    public static string Summarise(IReadOnlyList<(string Id, JudgeScore? Score)> results)
    {
        var sb = new StringBuilder();
        sb.Append("HomeScout answer-quality evaluation (model-graded, 1–5, pass ≥ 3)\n");
        sb.Append("================================================================\n");
        sb.Append($"Answers judged: {results.Count}\n\n");

        var scored = results.Where(r => r.Score is not null).Select(r => r.Score!).ToList();
        if (scored.Count > 0)
        {
            sb.Append($"  Relevance    avg {scored.Average(s => s.Relevance):0.0}\n");
            sb.Append($"  Usefulness   avg {scored.Average(s => s.Usefulness):0.0}\n");
            sb.Append($"  Groundedness avg {scored.Average(s => s.Groundedness):0.0}\n");
            sb.Append($"\n  Passed (all dimensions ≥ 3): {scored.Count(s => s.Passed())}/{scored.Count}\n");
        }

        var unscored = results.Count - scored.Count;
        if (unscored > 0)
        {
            sb.Append($"  Unscored (judge did not return a valid score): {unscored}\n");
        }

        sb.Append("\nPer answer:\n");
        foreach (var (id, score) in results)
        {
            sb.Append(score is null
                ? $"  [{id}] (no score)\n"
                : $"  [{id}] relevance {score.Relevance}, usefulness {score.Usefulness}, groundedness {score.Groundedness} — {score.Rationale}\n");
        }

        sb.Append($"\nResult: {(AllPassed(results) ? "PASS" : "REVIEW")}\n");
        return sb.ToString();
    }
}
