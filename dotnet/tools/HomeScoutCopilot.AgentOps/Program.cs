using Azure.Identity;
using HomeScoutCopilot.AgentOps;

// HomeScoutCopilot.AgentOps — deploy/manage HomeScout Foundry content.
//
//   agentops manifest [--out <path>]
//     Generate the declarative agent manifest from the single-sourced agent definition (the
//     versioned prompt asset + the tool set). Offline, verifiable. Model deployment name comes
//     from AZURE_FOUNDRY_MODEL_DEPLOYMENT (falls back to "chat"). Writes to --out, or stdout.
//
//   agentops deploy
//     Register the agent as a persisted, versioned Foundry agent (CreateAgentVersion) so it
//     appears in the portal as a named, versioned asset. Needs AZURE_FOUNDRY_PROJECT_ENDPOINT
//     (+ AZURE_FOUNDRY_MODEL_DEPLOYMENT) and Azure creds. External (billable) — verified live.

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

    case "deploy":
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_ENDPOINT");
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            Console.Error.WriteLine(
                "AZURE_FOUNDRY_PROJECT_ENDPOINT is not set — provision Foundry (azd provision) and sign in first.");
            return 2;
        }

        var model = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_MODEL_DEPLOYMENT");
        if (string.IsNullOrWhiteSpace(model))
        {
            model = "chat";
        }

        var definition = AgentManifest.Build(model);
        var deployer = new FoundryAgentDeployer(endpoint, new DefaultAzureCredential());
        var version = await deployer.DeployAsync(definition);

        Console.WriteLine(
            $"Registered agent '{version.Name}' version {version.Version} (id {version.Id}), " +
            $"prompt {definition.PromptVersion}, model {definition.Model}. Visible in the Foundry portal.");
        return 0;
    }

    default:
        Console.Error.WriteLine($"Unknown verb '{verb}'. Usage: agentops (manifest [--out <path>] | deploy)");
        return 1;
}
