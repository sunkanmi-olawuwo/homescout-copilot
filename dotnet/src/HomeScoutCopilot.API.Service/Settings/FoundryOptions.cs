using FluentValidation;

namespace HomeScoutCopilot.API.Service;

/// <summary>
/// Foundry connection settings. Populated from the azd provisioning outputs
/// (`AZURE_FOUNDRY_PROJECT_ENDPOINT`, `AZURE_FOUNDRY_MODEL_DEPLOYMENT`).
/// </summary>
public sealed class FoundryOptions : IValidatedOptions<FoundryOptions>
{
    public static string SectionName => "Foundry";

    /// <summary>The Foundry project endpoint URL.</summary>
    public string ProjectEndpoint { get; set; } = string.Empty;

    /// <summary>The chat model deployment name (a stable role label, e.g. "chat").</summary>
    public string ModelDeploymentName { get; set; } = "chat";

    public IValidator<FoundryOptions> GetValidator() => new Validator();

    private sealed class Validator : AbstractValidator<FoundryOptions>
    {
        public Validator()
        {
            RuleFor(x => x.ProjectEndpoint).NotEmpty();
            RuleFor(x => x.ModelDeploymentName).NotEmpty();
        }
    }
}
