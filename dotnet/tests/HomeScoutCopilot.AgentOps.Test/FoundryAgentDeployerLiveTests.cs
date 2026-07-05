using Azure.Identity;

namespace HomeScoutCopilot.AgentOps.Test;

// Live registration of the persisted, versioned Foundry agent (CreateAgentVersion). Makes a real
// Foundry call, so [Category("External")] + [Category("Integration")] — off the fast/blocking gate.
// Skips when Foundry isn't provisioned. CreateAgentVersion is idempotent on identical content, so
// re-runs don't spam versions.
[TestFixture]
[Category("External")]
[Category("Integration")]
public class FoundryAgentDeployerLiveTests
{
    [Test]
    public async Task Deploy_registers_the_versioned_agent()
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_ENDPOINT");
        var model = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_MODEL_DEPLOYMENT") ?? "chat";
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            Assert.Ignore("Foundry not provisioned (AZURE_FOUNDRY_PROJECT_ENDPOINT unset).");
        }

        var definition = AgentManifest.Build(model);
        var deployer = new FoundryAgentDeployer(endpoint!, new DefaultAzureCredential());

        var version = await deployer.DeployAsync(definition, TestContext.CurrentContext.CancellationToken);

        Assert.Multiple(() =>
        {
            Assert.That(version.Name, Is.EqualTo("HomeScout"));
            Assert.That(version.Version, Is.Not.Null.And.Not.Empty);
            Assert.That(version.Id, Is.Not.Null.And.Not.Empty);
        });
        TestContext.Out.WriteLine($"Registered {version.Name} v{version.Version} (id {version.Id}).");
    }
}
