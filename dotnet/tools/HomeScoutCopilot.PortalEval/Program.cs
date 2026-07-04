using System.ClientModel;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using OpenAI;
using OpenAI.Evals;

// HomeScoutCopilot.PortalEval — publish a model-graded evaluation RUN to the Foundry portal
// (BYO-responses) via the OpenAI Evals API against the Foundry /openai/v1 endpoint (keyless, Entra).
//
//   portaleval --data <answers.jsonl>
//     answers.jsonl: one {id, query, response} per line (generate with `evaluator answers --out`).
//     Creates an evaluation + a run over the provided answers, polls to completion, prints the run
//     id/status. Results appear in the Foundry portal's Evaluation tab (charts + run comparison).
//
// Isolated tool: references only the OpenAI/Azure.AI.OpenAI SDKs — no agent-graph coupling.

var dataIndex = Array.IndexOf(args, "--data");
var dataPath = dataIndex >= 0 && dataIndex + 1 < args.Length ? args[dataIndex + 1] : "answers.jsonl";

var projectEndpoint = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_ENDPOINT");
var judgeModel = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_JUDGE_DEPLOYMENT")
    ?? Environment.GetEnvironmentVariable("AZURE_FOUNDRY_MODEL_DEPLOYMENT");
if (string.IsNullOrWhiteSpace(projectEndpoint) || string.IsNullOrWhiteSpace(judgeModel))
{
    Console.Error.WriteLine(
        "Set AZURE_FOUNDRY_PROJECT_ENDPOINT + (AZURE_FOUNDRY_JUDGE_DEPLOYMENT or AZURE_FOUNDRY_MODEL_DEPLOYMENT).");
    return 2;
}

if (!File.Exists(dataPath))
{
    Console.Error.WriteLine($"Answers file not found: {dataPath}. Generate it with 'evaluator answers --out {dataPath}'.");
    return 1;
}

// {id,query,response} rows → OpenAI Evals "file_content" items.
var items = new List<object>();
foreach (var line in await File.ReadAllLinesAsync(dataPath))
{
    if (string.IsNullOrWhiteSpace(line))
    {
        continue;
    }

    using var doc = JsonDocument.Parse(line);
    var root = doc.RootElement;
    items.Add(new
    {
        item = new
        {
            query = root.GetProperty("query").GetString(),
            response = root.GetProperty("response").GetString(),
        },
    });
}

if (items.Count == 0)
{
    Console.Error.WriteLine("No answers to evaluate.");
    return 1;
}

// Keyless OpenAI Evals client against the Foundry OpenAI-compatible endpoint (/openai/v1), so the
// SDK's OpenAI-style paths (/evals) resolve. We pass an Entra access token as the bearer credential
// (the SDK sends "Authorization: Bearer <token>", which Foundry accepts) — no account keys. The
// token is valid ~1h, which comfortably covers one eval run; this is a one-shot ops tool.
var openAiEndpoint = new Uri($"{new Uri(projectEndpoint).GetLeftPart(UriPartial.Authority)}/openai/v1");
var accessToken = new DefaultAzureCredential().GetToken(
    new TokenRequestContext(["https://cognitiveservices.azure.com/.default"]), CancellationToken.None);
var evaluations = new EvaluationClient(
    new ApiKeyCredential(accessToken.Token), new OpenAIClientOptions { Endpoint = openAiEndpoint });

// 1) Create the evaluation: a custom {query,response} item schema + OpenAI-native `score_model`
// graders (LLM-as-judge with our rubric — the Foundry Evals endpoint accepts these, not Azure's
// azure_ai_evaluator/builtin.* types).
object ScoreGrader(string name, string instruction) => new
{
    type = "score_model",
    name,
    model = judgeModel,
    input = new object[]
    {
        new { role = "developer", content = $"{instruction} Respond with only an integer from 1 to 5 (5 = best)." },
        new { role = "user", content = "Question:\n{{ item.query }}\n\nAnswer:\n{{ item.response }}" },
    },
    range = new[] { 1, 5 },
    pass_threshold = 3,
};

var evaluationSpec = new
{
    name = "HomeScout answer quality (BYO-responses)",
    data_source_config = new
    {
        type = "custom",
        item_schema = new
        {
            type = "object",
            properties = new
            {
                query = new { type = "string" },
                response = new { type = "string" },
            },
            required = new[] { "query", "response" },
        },
        include_sample_schema = false,
    },
    testing_criteria = new[]
    {
        ScoreGrader("relevance", "Grade how directly and completely the answer addresses the question."),
        ScoreGrader("groundedness", "Grade whether the answer's figures and claims are consistent with its own stated assumptions (no invented or contradictory numbers)."),
    },
};

Console.WriteLine($"Creating evaluation + run over {items.Count} answers (judge: {judgeModel})…");
using var evalContent = BinaryContent.Create(BinaryData.FromObjectAsJson(evaluationSpec));
var evaluationResult = await evaluations.CreateEvaluationAsync(evalContent);
var evaluationId = ReadString(evaluationResult, "id")
    ?? throw new InvalidOperationException("Evaluation creation returned no id.");
Console.WriteLine($"Evaluation created: {evaluationId}");

// 2) Create the run with the provided answers (BYO — no completions target).
var runSpec = new
{
    name = "homescout-byo-run",
    data_source = new
    {
        type = "jsonl",
        source = new { type = "file_content", content = items },
    },
};

using var runContent = BinaryContent.Create(BinaryData.FromObjectAsJson(runSpec));
var runResult = await evaluations.CreateEvaluationRunAsync(evaluationId, runContent);
var runId = ReadString(runResult, "id") ?? throw new InvalidOperationException("Run creation returned no id.");
var status = ReadString(runResult, "status") ?? "unknown";
Console.WriteLine($"Run created: {runId} (status {status})");

// 3) Poll to a terminal state.
ClientResult polled = runResult;
for (var attempt = 0; attempt < 60 && status is not ("completed" or "failed" or "canceled"); attempt++)
{
    await Task.Delay(TimeSpan.FromSeconds(5));
    polled = await evaluations.GetEvaluationRunAsync(evaluationId, runId, options: null);
    status = ReadString(polled, "status") ?? status;
    Console.WriteLine($"  … {status}");
}

Console.WriteLine();
Console.WriteLine($"Evaluation run '{runId}' finished with status '{status}'. {ResultCounts(polled)}");
Console.WriteLine($"View it in the Foundry portal → Evaluation tab (evaluation {evaluationId}).");
return status == "completed" ? 0 : 1;

static string? ReadString(ClientResult result, string property)
{
    using var doc = JsonDocument.Parse(result.GetRawResponse().Content.ToString());
    return doc.RootElement.TryGetProperty(property, out var value) ? value.GetString() : null;
}

static string ResultCounts(ClientResult result)
{
    using var doc = JsonDocument.Parse(result.GetRawResponse().Content.ToString());
    if (!doc.RootElement.TryGetProperty("result_counts", out var counts))
    {
        return string.Empty;
    }

    int Get(string p) => counts.TryGetProperty(p, out var v) && v.TryGetInt32(out var n) ? n : 0;
    return $"Results — passed {Get("passed")}, failed {Get("failed")}, errored {Get("errored")}, total {Get("total")}.";
}
