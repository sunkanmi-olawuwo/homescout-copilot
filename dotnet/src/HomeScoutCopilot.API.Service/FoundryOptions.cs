namespace HomeScoutCopilot.API.Service;

/// <summary>
/// Foundry connection settings. Populated from the azd provisioning outputs
/// (`AZURE_FOUNDRY_PROJECT_ENDPOINT`, `AZURE_FOUNDRY_MODEL_DEPLOYMENT`).
/// </summary>
public sealed class FoundryOptions
{
    public const string SectionName = "Foundry";

    /// <summary>The Foundry project endpoint URL.</summary>
    public string ProjectEndpoint { get; set; } = string.Empty;

    /// <summary>The chat model deployment name (a stable role label, e.g. "chat").</summary>
    public string ModelDeploymentName { get; set; } = "chat";
}
