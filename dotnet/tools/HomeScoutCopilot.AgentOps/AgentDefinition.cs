namespace HomeScoutCopilot.AgentOps;

/// <summary>
/// The HomeScout agent, assembled from the single-sourced pieces: the versioned prompt
/// asset, the tool set, and the model deployment. This is what a deploy step registers as
/// a versioned Foundry agent; today it is serialised to the declarative agent manifest.
/// </summary>
/// <param name="Name">The agent name (stable across versions).</param>
/// <param name="Model">The model deployment name (environment-specific).</param>
/// <param name="PromptVersion">The prompt asset version (e.g. "v1").</param>
/// <param name="InstructionsFile">Repo path of the prompt asset the instructions come from.</param>
/// <param name="Instructions">The resolved system instructions.</param>
/// <param name="ToolNames">The tools the agent exposes.</param>
public sealed record AgentDefinition(
    string Name,
    string Model,
    string PromptVersion,
    string InstructionsFile,
    string Instructions,
    IReadOnlyList<string> ToolNames);
