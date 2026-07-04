using HomeScoutCopilot.AgentOps;
using HomeScoutCopilot.API.Service;

namespace HomeScoutCopilot.AgentOps.Test;

// Locks the declarative agent manifest: it is assembled from the single-sourced agent
// definition (versioned prompt asset + tool names) and serialises to the expected shape.
// This is the offline behaviour-lock for the deploy step; live CreateAgentVersion is separate.
[TestFixture]
public class AgentManifestTests
{
    [Test]
    public void Build_assembles_the_definition_from_the_single_source()
    {
        var definition = AgentManifest.Build("gpt-4.1-mini");

        Assert.Multiple(() =>
        {
            Assert.That(definition.Name, Is.EqualTo("HomeScout"));
            Assert.That(definition.Model, Is.EqualTo("gpt-4.1-mini"));
            Assert.That(definition.PromptVersion, Is.EqualTo(AgentPrompt.Version));
            Assert.That(definition.InstructionsFile, Is.EqualTo($"Prompts/homescout.{AgentPrompt.Version}.md"));
            Assert.That(definition.Instructions, Is.EqualTo(AgentPrompt.Instructions));
            Assert.That(definition.Instructions, Does.Contain("not a mortgage adviser"));
            Assert.That(definition.ToolNames, Is.EqualTo(HomeScoutAgentTools.ToolNames));
            Assert.That(definition.ToolNames, Does.Contain("estimate_mortgage").And.Contains("get_base_rate"));
        });
    }

    [Test]
    public void ToYaml_emits_the_declarative_manifest_shape()
    {
        var yaml = AgentManifest.ToYaml(AgentManifest.Build("gpt-4.1-mini"));

        Assert.Multiple(() =>
        {
            Assert.That(yaml, Does.Contain("name: HomeScout"));
            Assert.That(yaml, Does.Contain("model: gpt-4.1-mini"));
            Assert.That(yaml, Does.Contain($"instructions_file: Prompts/homescout.{AgentPrompt.Version}.md"));
            Assert.That(yaml, Does.Contain("tools:"));
            Assert.That(yaml, Does.Contain("  - estimate_mortgage"));
            Assert.That(yaml, Does.Contain("  - get_base_rate"));
        });
    }

    // Drift guard: the committed manifest is derived from the prompt + tools, so it must stay
    // in step with the code. If someone changes the prompt/tool set without regenerating, this
    // fails with the exact command to fix it.
    [Test]
    public void Committed_manifest_asset_is_up_to_date()
    {
        var path = Path.Combine(
            FindRepoRoot(),
            "dotnet", "src", "HomeScoutCopilot.API.Service", "Prompts", "homescout.agent.yaml");

        Assert.That(File.Exists(path), Is.True, $"manifest asset missing at {path}");

        var committed = File.ReadAllText(path).Replace("\r\n", "\n");
        var expected = AgentManifest.ToYaml(AgentManifest.Build("chat")).Replace("\r\n", "\n");

        Assert.That(committed, Is.EqualTo(expected),
            "homescout.agent.yaml is stale — regenerate: dotnet run --project " +
            "dotnet/tools/HomeScoutCopilot.AgentOps -- manifest --out " +
            "dotnet/src/HomeScoutCopilot.API.Service/Prompts/homescout.agent.yaml");
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
