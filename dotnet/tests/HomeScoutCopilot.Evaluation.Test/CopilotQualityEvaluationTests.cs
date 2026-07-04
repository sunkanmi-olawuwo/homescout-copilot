using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Evaluator;
using HomeScoutCopilot.Shared.Contracts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.DependencyInjection;

namespace HomeScoutCopilot.Evaluation.Test;

/// <summary>
/// The standard-library evaluation harness: asks the LIVE copilot each dataset query, then scores
/// each real answer with Microsoft's built-in quality evaluators AND HomeScout's bespoke judge +
/// guardrails, persisting the results for a regression-tracked <c>dotnet aieval</c> report. Every
/// run makes real model calls, so [Category("External")] + [Category("Integration")] keep it off
/// the fast/blocking gate. Skips cleanly when Foundry isn't provisioned.
/// </summary>
[TestFixture]
[Category("External")]
[Category("Integration")]
public class CopilotQualityEvaluationTests
{
    private ServiceProvider? _gatewayProvider;
    private ChatConfiguration? _chatConfiguration;
    private ReportingConfiguration? _reportingConfiguration;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _chatConfiguration = FoundryChatFactory.TryCreate();
        _gatewayProvider = CopilotGatewayFactory.TryBuild();
        if (_chatConfiguration is not null)
        {
            _reportingConfiguration = HomeScoutReportingConfiguration.Create(_chatConfiguration);
        }
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => _gatewayProvider?.Dispose();

    [Test]
    public async Task Copilot_answers_are_evaluated_and_guardrails_hold()
    {
        if (_gatewayProvider is null || _reportingConfiguration is null)
        {
            Assert.Ignore("Foundry not provisioned (AZURE_FOUNDRY_PROJECT_ENDPOINT / AZURE_FOUNDRY_MODEL_DEPLOYMENT unset).");
        }

        var dataPath = Path.Combine(AppContext.BaseDirectory, "data", "homescout-eval.jsonl");
        var cases = EvaluationDataset.Load(dataPath);
        Assert.That(cases, Is.Not.Empty, "the eval dataset should not be empty");

        var guardrailFailures = new List<string>();
        var evaluatorErrors = new List<string>();

        foreach (var scenario in cases)
        {
            // Ask the live copilot for the real product answer (scoped, like the API).
            CopilotAnswer answer;
            using (var scope = _gatewayProvider!.CreateScope())
            {
                var gateway = scope.ServiceProvider.GetRequiredService<IHomeScoutAgentGateway>();
                answer = await gateway.AskAsync(new CopilotRequest(scenario.Query));
            }

            var messages = new List<ChatMessage> { new(ChatRole.User, scenario.Query) };
            var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, answer.Text));

            await using var scenarioRun = await _reportingConfiguration!.CreateScenarioRunAsync(
                scenarioName: $"HomeScout.Copilot.{scenario.Id}",
                additionalTags: ["copilot", "quality"]);

            var result = await scenarioRun.EvaluateAsync(messages, response);

            // Log the quality scores (tracked as trends via the report — not a hard gate).
            LogQuality(scenario.Id, result);

            // Every model-graded evaluator must actually have run. A *blocking* error — the judge
            // model rejecting the request shape (a 400), or an auth/config failure — means the
            // integration is broken, so fail loudly rather than let a green run hide it. A
            // *non-blocking* error — throttling, a brief outage, or the reasoning judge model
            // returning an unparseable score on a given call — is inherent LLM-as-judge variance
            // and must not block per our standards; log it and rely on the trend across runs.
            foreach (var (name, metric) in result.Metrics)
            {
                var error = metric.Diagnostics?
                    .FirstOrDefault(d => d.Severity == EvaluationDiagnosticSeverity.Error)?.Message;
                if (error is null)
                {
                    continue;
                }

                var firstLine = error.Split('\n')[0];
                if (IsNonBlocking(error))
                {
                    TestContext.Out.WriteLine($"[non-blocking] [{scenario.Id}] {name}: {firstLine}");
                }
                else
                {
                    evaluatorErrors.Add($"[{scenario.Id}] {name}: {firstLine}");
                }
            }

            // Guardrails ARE a hard product requirement: any failure fails the run.
            foreach (var name in new HomeScoutGuardrailEvaluator().EvaluationMetricNames)
            {
                var metric = result.Get<BooleanMetric>(name);
                if (metric.Interpretation?.Failed == true)
                {
                    guardrailFailures.Add($"[{scenario.Id}] {name}: {metric.Reason}");
                }
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(evaluatorErrors, Is.Empty,
                "model-graded evaluators must run without errors:\n" + string.Join("\n", evaluatorErrors));
            Assert.That(guardrailFailures, Is.Empty,
                "the copilot must not violate HomeScout's product guardrails:\n" + string.Join("\n", guardrailFailures));
        });

        TestContext.Out.WriteLine(
            $"Evaluation results persisted to {HomeScoutReportingConfiguration.StorageRootPath} " +
            $"(execution '{HomeScoutReportingConfiguration.ResolveExecutionName()}'). " +
            "Generate the report with: dotnet tool run aieval report --path <store> --output report.html");
    }

    // Non-blocking: throttling / brief outages, and the reasoning judge model occasionally returning
    // a response the strict built-in evaluators can't parse — both are expected live-LLM variance.
    // A 400 (bad request shape) or auth failure is a real integration bug and must block.
    private static bool IsNonBlocking(string error) =>
        error.Contains("429") || error.Contains("(503)") || error.Contains("(500)") ||
        error.Contains("(502)") || error.Contains("(504)") ||
        error.Contains("timed out", StringComparison.OrdinalIgnoreCase) ||
        error.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
        error.Contains("temporarily", StringComparison.OrdinalIgnoreCase) ||
        error.Contains("Failed to parse", StringComparison.OrdinalIgnoreCase);

    private static void LogQuality(string id, EvaluationResult result)
    {
        foreach (var metricName in new[]
                 {
                     RelevanceEvaluator.RelevanceMetricName,
                     CoherenceEvaluator.CoherenceMetricName,
                     FluencyEvaluator.FluencyMetricName,
                     HomeScoutBespokeJudgeEvaluator.RelevanceMetric,
                     HomeScoutBespokeJudgeEvaluator.UsefulnessMetric,
                     HomeScoutBespokeJudgeEvaluator.GroundednessMetric,
                 })
        {
            if (result.TryGet<NumericMetric>(metricName, out var metric))
            {
                TestContext.Out.WriteLine($"[{id}] {metricName}: {metric.Value?.ToString() ?? "—"} ({metric.Interpretation?.Rating})");
            }
        }
    }
}
