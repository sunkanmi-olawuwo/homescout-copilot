using System.Text.Json;
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
//   evaluator multiturn[--data <path>] drive each multi-turn conversation (ordered turns) against
//                                      the LIVE copilot over ONE session and assert the final answer
//                                      carried context (defaults to data/homescout-multiturn-eval.jsonl).
//                                      Needs AZURE_FOUNDRY_* + Azure creds (external). Exit 1 on any
//                                      conversation that lost context.

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

    case "answers":
    {
        // Generate BYO-responses: ask the live copilot each query and write {id,query,response}
        // JSONL, for the isolated PortalEval tool to publish to the Foundry portal.
        var provider = CopilotGatewayFactory.TryBuild();
        if (provider is null)
        {
            Console.Error.WriteLine(
                "Foundry not configured — set AZURE_FOUNDRY_PROJECT_ENDPOINT + AZURE_FOUNDRY_MODEL_DEPLOYMENT.");
            return 2;
        }

        await using (provider)
        {
            if (!File.Exists(dataPath))
            {
                Console.Error.WriteLine($"Eval dataset not found: {dataPath}");
                return 1;
            }

            var outIndex = Array.IndexOf(args, "--out");
            var outPath = outIndex >= 0 && outIndex + 1 < args.Length ? args[outIndex + 1] : "answers.jsonl";

            var gateway = provider.GetRequiredService<IHomeScoutAgentGateway>();
            var cases = EvaluationDataset.Load(dataPath);
            Console.WriteLine($"Asking the live copilot {cases.Count} question(s), writing {outPath}…");
            var liveCases = await LiveEvaluation.GenerateAsync(gateway, cases);

            await using var writer = new StreamWriter(outPath);
            foreach (var scenario in liveCases)
            {
                await writer.WriteLineAsync(JsonSerializer.Serialize(
                    new { id = scenario.Id, query = scenario.Query, response = scenario.Response }));
            }

            Console.WriteLine($"Wrote {liveCases.Count} answers to {outPath}.");
            return 0;
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
            // Judge on a dedicated, higher-capability deployment when provisioned (avoid self-judging);
            // the copilot still generates on the chat deployment via the gateway.
            var judgeModel = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_JUDGE_DEPLOYMENT") ?? model;
            var judge = new FoundryAnswerJudge(endpoint, judgeModel, new Azure.Identity.DefaultAzureCredential());
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

    case "multiturn":
    {
        var provider = CopilotGatewayFactory.TryBuild();
        if (provider is null)
        {
            Console.Error.WriteLine(
                "Foundry not configured — set AZURE_FOUNDRY_PROJECT_ENDPOINT + AZURE_FOUNDRY_MODEL_DEPLOYMENT " +
                "(and sign in with Azure creds).");
            return 2;
        }

        await using (provider)
        {
            // Multi-turn has its own default dataset (ordered turns), unless --data overrides it.
            var multiTurnPath = dataIndex >= 0 && dataIndex + 1 < args.Length
                ? args[dataIndex + 1]
                : Path.Combine(AppContext.BaseDirectory, "data", "homescout-multiturn-eval.jsonl");
            if (!File.Exists(multiTurnPath))
            {
                Console.Error.WriteLine($"Multi-turn eval dataset not found: {multiTurnPath}");
                return 1;
            }

            var gateway = provider.GetRequiredService<IHomeScoutAgentGateway>();
            var cases = MultiTurnDataset.Load(multiTurnPath);
            Console.WriteLine($"Driving {cases.Count} multi-turn conversation(s) against the live copilot…");
            var results = await MultiTurnEvaluation.RunAsync(gateway, cases);
            Console.Write(MultiTurnEvaluation.Summarise(results));
            return MultiTurnEvaluation.AllCarried(results) ? 0 : 1;
        }
    }

    default:
        Console.Error.WriteLine($"Unknown verb '{verb}'. Usage: evaluator (safety|run|quality|multiturn) [--data <path>]");
        return 1;
}
