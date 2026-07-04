using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Evaluator;
using Microsoft.Extensions.DependencyInjection;

// HomeScoutCopilot.Evaluator — measure the copilot's guardrail adherence.
//
//   evaluator safety [--data <path>]   run the deterministic HomeScout safety evaluators
//                                      (not-mortgage-advice, no product recommendation, no
//                                      safe/unsafe area verdict) over the dataset's authored
//                                      responses. Offline, exit 1 on any failure.
//   evaluator run    [--data <path>]   ask the LIVE copilot each dataset query, then run the
//                                      same safety evaluators over the real answers. Needs
//                                      AZURE_FOUNDRY_* + Azure creds (external).
//
// Model-graded Foundry cloud evals (intent / relevance / groundedness) are a separate,
// live-verified step — see genaiops-tooling-plan.

var verb = args.Length > 0 ? args[0] : "safety";
var dataIndex = Array.IndexOf(args, "--data");
var dataPath = dataIndex >= 0 && dataIndex + 1 < args.Length
    ? args[dataIndex + 1]
    : Path.Combine(AppContext.BaseDirectory, "data", "homescout-eval.jsonl");

switch (verb)
{
    case "safety":
    {
        if (!File.Exists(dataPath))
        {
            Console.Error.WriteLine($"Eval dataset not found: {dataPath}");
            return 1;
        }

        var cases = EvaluationDataset.Load(dataPath);
        var results = EvaluationRunner.Run(cases, SafetyEvaluators.All);
        Console.Write(EvaluationRunner.Summarise(results));
        return EvaluationRunner.AllPassed(results) ? 0 : 1;
    }

    case "run":
    {
        var provider = CopilotGatewayFactory.TryBuild();
        if (provider is null)
        {
            Console.Error.WriteLine(
                "Foundry not configured — set AZURE_FOUNDRY_PROJECT_ENDPOINT + AZURE_FOUNDRY_MODEL_DEPLOYMENT " +
                "(and sign in with Azure creds), or run 'safety' for the offline dataset check.");
            return 2;
        }

        await using (provider)
        {
            if (!File.Exists(dataPath))
            {
                Console.Error.WriteLine($"Eval dataset not found: {dataPath}");
                return 1;
            }

            var gateway = provider.GetRequiredService<IHomeScoutAgentGateway>();
            var cases = EvaluationDataset.Load(dataPath);
            Console.WriteLine($"Asking the live copilot {cases.Count} question(s)…");
            var liveCases = await LiveEvaluation.GenerateAsync(gateway, cases);
            var results = EvaluationRunner.Run(liveCases, SafetyEvaluators.All);
            Console.Write(EvaluationRunner.Summarise(results));
            return EvaluationRunner.AllPassed(results) ? 0 : 1;
        }
    }

    default:
        Console.Error.WriteLine($"Unknown verb '{verb}'. Usage: evaluator (safety|run) [--data <path>]");
        return 1;
}
