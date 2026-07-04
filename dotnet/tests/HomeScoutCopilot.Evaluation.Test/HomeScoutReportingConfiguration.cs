using Azure.Identity;
using Azure.Storage.Files.DataLake;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using Microsoft.Extensions.AI.Evaluation.Safety;

namespace HomeScoutCopilot.Evaluation.Test;

/// <summary>
/// Assembles the single <see cref="ReportingConfiguration"/> for HomeScout's answer-quality
/// evaluation. The evaluator set is deliberately mixed and each metric is labelled by origin:
/// Microsoft's research-validated quality evaluators (Relevance, Coherence, Fluency) run alongside
/// HomeScout's own bespoke judge and deterministic guardrails, so one report compares them directly.
/// When content-safety is enabled, Foundry's content-harm evaluators join the same run. Results are
/// persisted (disk by default, Azure ADLS Gen2 when configured) keyed by an execution name so scores
/// can be tracked across runs for regression history.
/// </summary>
public static class HomeScoutReportingConfiguration
{
    /// <summary>
    /// The set of evaluators applied to every copilot answer, labelled by origin. Content-harm
    /// evaluators (Foundry safety service, billable per call) are added only when opted in via
    /// <c>AZURE_EVAL_CONTENT_SAFETY</c> — off the fast/blocking gate.
    /// </summary>
    public static IEnumerable<IEvaluator> BuildEvaluators(bool includeContentSafety)
    {
        var evaluators = new List<IEvaluator>
        {
            // Microsoft.Extensions.AI.Evaluation — research-validated, LLM-graded quality metrics.
            new RelevanceEvaluator(),
            new CoherenceEvaluator(),
            new FluencyEvaluator(),
            // HomeScout's own — kept alongside for an explicit side-by-side comparison.
            new HomeScoutBespokeJudgeEvaluator(),
            new HomeScoutGuardrailEvaluator(),
        };

        if (includeContentSafety)
        {
            // Foundry content-harm evaluators (via the safety service). Complement — not replace —
            // our domain guardrails; there is no built-in evaluator for "not mortgage advice".
            evaluators.Add(new HateAndUnfairnessEvaluator());
            evaluators.Add(new ViolenceEvaluator());
            evaluators.Add(new SelfHarmEvaluator());
            evaluators.Add(new SexualEvaluator());
        }

        return evaluators;
    }

    /// <summary>True when the Foundry content-safety evaluators are opted in.</summary>
    public static bool ContentSafetyEnabled =>
        Environment.GetEnvironmentVariable("AZURE_EVAL_CONTENT_SAFETY") is "1" or "true";

    /// <summary>
    /// A stable name that groups the results of one run. Prefers the CI build number so scores
    /// line up across builds for regression tracking; falls back to a timestamp locally.
    /// </summary>
    public static string ResolveExecutionName() =>
        Environment.GetEnvironmentVariable("BUILD_BUILDNUMBER")
        ?? Environment.GetEnvironmentVariable("GITHUB_RUN_NUMBER")
        ?? $"local-{DateTime.UtcNow:yyyyMMddTHHmmss}";

    /// <summary>The on-disk (or CI-artifact) store the <c>dotnet aieval</c> report is generated from.</summary>
    public static string StorageRootPath =>
        Environment.GetEnvironmentVariable("AZURE_EVAL_STORAGE_PATH")
        ?? Path.Combine(Path.GetTempPath(), "homescout-eval-store");

    /// <summary>True when an Azure (ADLS Gen2) evaluation store is configured for cloud persistence.</summary>
    public static bool UsesCloudStore =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AZURE_EVAL_STORAGE_ENDPOINT"));

    /// <summary>
    /// Builds the reporting configuration. When <c>AZURE_EVAL_STORAGE_ENDPOINT</c> (an ADLS Gen2
    /// <c>https://&lt;account&gt;.dfs.core.windows.net</c> URL) is set, results + cached responses are
    /// persisted to Azure Storage — keyless, keyed by execution name — for cloud regression history
    /// and shareable reports. Otherwise they go to the local disk store. The evaluator set and the
    /// judge model are identical either way, so local and cloud runs are directly comparable.
    /// </summary>
    public static ReportingConfiguration Create(ChatConfiguration chatConfiguration)
    {
        var includeContentSafety = ContentSafetyEnabled;
        var evaluators = BuildEvaluators(includeContentSafety);

        // The content-safety evaluators reach the Foundry safety service via a ChatConfiguration
        // that also carries the judge chat client, so quality + safety run in one configuration.
        var effectiveChatConfiguration = includeContentSafety
            ? BuildContentSafetyConfiguration().ToChatConfiguration(chatConfiguration)
            : chatConfiguration;

        return UsesCloudStore
            ? CreateAzure(evaluators, effectiveChatConfiguration)
            : DiskBasedReportingConfiguration.Create(
                storageRootPath: StorageRootPath,
                evaluators: evaluators,
                chatConfiguration: effectiveChatConfiguration,
                enableResponseCaching: true,
                executionName: ResolveExecutionName());
    }

    private static ContentSafetyServiceConfiguration BuildContentSafetyConfiguration()
    {
        // Non-Hub Foundry project: the safety service is addressed by the project endpoint.
        var projectEndpoint = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_ENDPOINT")
            ?? throw new InvalidOperationException(
                "AZURE_EVAL_CONTENT_SAFETY is set but AZURE_FOUNDRY_PROJECT_ENDPOINT is not.");
        return new ContentSafetyServiceConfiguration(new DefaultAzureCredential(), new Uri(projectEndpoint));
    }

    private static ReportingConfiguration CreateAzure(
        IEnumerable<IEvaluator> evaluators, ChatConfiguration chatConfiguration)
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_EVAL_STORAGE_ENDPOINT")!;
        var fileSystem = Environment.GetEnvironmentVariable("AZURE_EVAL_STORAGE_FILESYSTEM") ?? "evaluations";
        var directory = Environment.GetEnvironmentVariable("AZURE_EVAL_STORAGE_DIRECTORY") ?? "homescout";

        var service = new DataLakeServiceClient(new Uri(endpoint), new DefaultAzureCredential());
        var directoryClient = service.GetFileSystemClient(fileSystem).GetDirectoryClient(directory);

        return AzureStorageReportingConfiguration.Create(
            client: directoryClient,
            evaluators: evaluators,
            chatConfiguration: chatConfiguration,
            enableResponseCaching: true,
            executionName: ResolveExecutionName());
    }
}
