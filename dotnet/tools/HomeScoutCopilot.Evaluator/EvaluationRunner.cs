using System.Text;

namespace HomeScoutCopilot.Evaluator;

/// <summary>Runs the safety evaluators over an eval dataset and summarises pass rates + failures.</summary>
public static class EvaluationRunner
{
    public static IReadOnlyList<CaseResult> Run(
        IEnumerable<EvaluationCase> cases,
        IReadOnlyList<Func<string, EvaluatorResult>> evaluators)
    {
        return cases
            .Select(c => new CaseResult(c.Id, evaluators.Select(e => e(c.Response)).ToList()))
            .ToList();
    }

    /// <summary>True when every case passed every evaluator.</summary>
    public static bool AllPassed(IEnumerable<CaseResult> results) => results.All(r => r.Passed);

    public static string Summarise(IReadOnlyList<CaseResult> results)
    {
        var sb = new StringBuilder();
        sb.Append("HomeScout safety evaluation\n");
        sb.Append("===========================\n");
        sb.Append($"Cases: {results.Count}\n\n");

        // Per-evaluator pass rate.
        var byEvaluator = results
            .SelectMany(r => r.Results)
            .GroupBy(r => r.Evaluator)
            .OrderBy(g => g.Key);
        foreach (var group in byEvaluator)
        {
            var passed = group.Count(r => r.Passed);
            var total = group.Count();
            sb.Append($"  {group.Key,-32} {passed}/{total} passed\n");
        }

        // Failures, if any.
        var failures = results.Where(r => !r.Passed).ToList();
        if (failures.Count > 0)
        {
            sb.Append($"\nFailures ({failures.Count}):\n");
            foreach (var caseResult in failures)
            {
                foreach (var failed in caseResult.Results.Where(r => !r.Passed))
                {
                    sb.Append($"  [{caseResult.Id}] {failed.Evaluator}: {failed.Detail}\n");
                }
            }
        }

        var casesPassed = results.Count(r => r.Passed);
        sb.Append($"\nResult: {casesPassed}/{results.Count} cases passed all evaluators — ");
        sb.Append(AllPassed(results) ? "PASS\n" : "FAIL\n");
        return sb.ToString();
    }
}
