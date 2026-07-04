using HomeScoutCopilot.AgentOps;

// HomeScoutCopilot.AgentOps — deploy/manage HomeScout Foundry content.
//
// Today: generate the declarative agent manifest from the single-sourced agent definition
// (the versioned prompt asset + the tool set). This is the offline, verifiable half of the
// deploy step. Live registration of a versioned Foundry agent (CreateAgentVersion) is a
// separate, live-verified slice — see wiki/__plans/03-backend/genaiops-tooling-plan.md.
//
// Usage:
//   agentops manifest [--out <path>]
//     Model deployment name comes from AZURE_FOUNDRY_MODEL_DEPLOYMENT (falls back to "chat",
//     matching FoundryOptions' default role label). Writes to --out, or stdout if omitted.

var verb = args.Length > 0 ? args[0] : "manifest";

switch (verb)
{
    case "manifest":
    {
        var model = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_MODEL_DEPLOYMENT");
        if (string.IsNullOrWhiteSpace(model))
        {
            model = "chat";
        }

        var definition = AgentManifest.Build(model);
        var yaml = AgentManifest.ToYaml(definition);

        var outIndex = Array.IndexOf(args, "--out");
        if (outIndex >= 0 && outIndex + 1 < args.Length)
        {
            var path = args[outIndex + 1];
            File.WriteAllText(path, yaml);
            Console.WriteLine(
                $"Wrote {definition.Name} manifest (prompt {definition.PromptVersion}, model {definition.Model}) to {path}");
        }
        else
        {
            Console.Write(yaml);
        }

        return 0;
    }

    default:
        Console.Error.WriteLine($"Unknown verb '{verb}'. Usage: agentops manifest [--out <path>]");
        Console.Error.WriteLine(
            "  Live deploy (CreateAgentVersion) is a separate, live-verified slice — see genaiops-tooling-plan.");
        return 1;
}
