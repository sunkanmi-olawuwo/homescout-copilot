using HomeScoutCopilot.Evaluator;

// HomeScoutCopilot.Evaluator — measure the copilot's guardrail adherence.
//
// Today: `evaluator safety [--data <path>]` runs the deterministic HomeScout safety evaluators
// (not-mortgage-advice, no product recommendation, no safe/unsafe area verdict) over the
// version-controlled eval dataset and reports pass rates + failures. Exit code 1 on any failure.
//
// Model-graded Foundry cloud evals (intent / relevance / groundedness, over live copilot
// responses) are a separate, live-verified step — see genaiops-tooling-plan.

var verb = args.Length > 0 ? args[0] : "safety";

switch (verb)
{
    case "safety":
    {
        var dataIndex = Array.IndexOf(args, "--data");
        var path = dataIndex >= 0 && dataIndex + 1 < args.Length
            ? args[dataIndex + 1]
            : Path.Combine(AppContext.BaseDirectory, "data", "homescout-eval.jsonl");

        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"Eval dataset not found: {path}");
            return 1;
        }

        var cases = EvaluationDataset.Load(path);
        var results = EvaluationRunner.Run(cases, SafetyEvaluators.All);
        Console.Write(EvaluationRunner.Summarise(results));
        return EvaluationRunner.AllPassed(results) ? 0 : 1;
    }

    default:
        Console.Error.WriteLine($"Unknown verb '{verb}'. Usage: evaluator safety [--data <path>]");
        Console.Error.WriteLine(
            "  Model-graded Foundry cloud evals are a separate, live-verified step — see genaiops-tooling-plan.");
        return 1;
}
