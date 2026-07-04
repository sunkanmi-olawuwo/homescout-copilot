using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Evaluator;
using HomeScoutCopilot.Shared.Contracts;
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
//   evaluator quality[--data <path>]   ask the LIVE copilot each query, then an LLM judge
//                                      scores each real answer on relevance / usefulness /
//                                      groundedness (1–5, pass ≥ 3). Needs AZURE_FOUNDRY_* +
//                                      Azure creds (external). Exit 1 if any answer scores < 3.

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

    case "quality":
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_ENDPOINT");
        var model = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_MODEL_DEPLOYMENT");
        var provider = CopilotGatewayFactory.TryBuild();
        if (provider is null || string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(model))
        {
            Console.Error.WriteLine(
                "Foundry not configured — set AZURE_FOUNDRY_PROJECT_ENDPOINT + AZURE_FOUNDRY_MODEL_DEPLOYMENT " +
                "(and sign in with Azure creds).");
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
            var judge = new FoundryAnswerJudge(endpoint, model, new Azure.Identity.DefaultAzureCredential());
            var cases = EvaluationDataset.Load(dataPath);
            Console.WriteLine($"Asking + model-grading {cases.Count} live copilot answer(s)…");

            var results = new List<(string Id, JudgeScore? Score)>();
            foreach (var scenario in cases)
            {
                var answer = await gateway.AskAsync(new CopilotRequest(scenario.Query));
                var score = await judge.JudgeAsync(scenario.Query, answer.Text);
                results.Add((scenario.Id, score));
            }

            Console.Write(QualityReport.Summarise(results));
            return QualityReport.AllPassed(results) ? 0 : 1;
        }
    }

    default:
        Console.Error.WriteLine($"Unknown verb '{verb}'. Usage: evaluator (safety|run|quality) [--data <path>]");
        return 1;
}
