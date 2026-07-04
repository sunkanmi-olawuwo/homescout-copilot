using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Evaluator;
using Microsoft.Extensions.DependencyInjection;

namespace HomeScoutCopilot.Evaluator.Test;

// Live: drives the committed multi-turn dataset against the real Foundry-backed copilot over one
// session per conversation and asserts each carried context. [Category("External")] (nightly) +
// [Category("Integration")] (excluded from the fast gate); skips when Foundry isn't provisioned.
[TestFixture]
[Category("Integration")]
[Category("External")]
public class MultiTurnLiveTests
{
    [Test]
    public async Task Committed_multiturn_conversations_carry_context_live()
    {
        var provider = CopilotGatewayFactory.TryBuild();
        if (provider is null)
        {
            Assert.Ignore("Foundry not provisioned (AZURE_FOUNDRY_PROJECT_ENDPOINT / AZURE_FOUNDRY_MODEL_DEPLOYMENT unset).");
        }

        await using (provider)
        {
            var gateway = provider!.GetRequiredService<IHomeScoutAgentGateway>();
            var path = Path.Combine(
                FindRepoRoot(),
                "dotnet", "tools", "HomeScoutCopilot.Evaluator", "data", "homescout-multiturn-eval.jsonl");
            var cases = MultiTurnDataset.Load(path);

            var results = await MultiTurnEvaluation.RunAsync(gateway, cases);

            Assert.That(MultiTurnEvaluation.AllCarried(results), Is.True, MultiTurnEvaluation.Summarise(results));
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "dotnet", "HomeScoutCopilot.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Could not locate the repo root (HomeScoutCopilot.slnx).");
    }
}
