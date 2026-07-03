using Azure.Identity;
using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Shared.Contracts;
using Microsoft.Extensions.Options;

namespace HomeScoutCopilot.API.Test;

// Live verification that the Foundry agent actually calls our tools. Makes a real call
// to a provisioned Foundry project, so it is [Category("External")] (nightly) +
// [Category("Integration")] (excluded from the fast PR gate). Skips cleanly when the
// Foundry env vars aren't set — it only runs where provisioning + Azure creds exist.
//
//   AZURE_FOUNDRY_PROJECT_ENDPOINT=... AZURE_FOUNDRY_MODEL_DEPLOYMENT=chat \
//     dotnet test --filter "FullyQualifiedName~FoundryAgentGatewayLiveTests"
//
// A failure here means the live copilot path is broken; a pass means the model really
// used our deterministic tool.
[TestFixture]
[Category("Integration")]
[Category("External")]
public class FoundryAgentGatewayLiveTests
{
    [Test]
    public async Task Copilot_answers_a_cost_question_by_calling_the_mortgage_tool()
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_ENDPOINT");
        var model = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_MODEL_DEPLOYMENT");
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(model))
        {
            Assert.Ignore("Foundry not provisioned (AZURE_FOUNDRY_PROJECT_ENDPOINT / AZURE_FOUNDRY_MODEL_DEPLOYMENT unset).");
        }

        var options = Options.Create(new FoundryOptions
        {
            ProjectEndpoint = endpoint!,
            ModelDeploymentName = model!,
        });
        var tools = new HomeScoutAgentTools(
            new MortgageCostEstimator(),
            new StubBaseRateProvider(
                new BaseRate(3.75m, new DateOnly(2026, 6, 19), "Fallback", "Bank of England", "Context only.")));
        var gateway = new FoundryAgentGateway(options, new DefaultAzureCredential(), tools);

        var answer = await gateway.AskAsync(new CopilotRequest(
            "What would the monthly cost be on a £300,000 flat with a 10% deposit at 4.5% over 25 years?"));

        Assert.Multiple(() =>
        {
            Assert.That(answer.Text, Is.Not.Empty);
            Assert.That(
                answer.ToolCalls.Any(t => t.Name == "estimate_mortgage"),
                Is.True,
                "the agent should have called the estimate_mortgage tool");
            Assert.That(
                answer.Caveats.Any(c => c.Contains("not mortgage advice", StringComparison.OrdinalIgnoreCase)),
                Is.True);
        });
    }
}
