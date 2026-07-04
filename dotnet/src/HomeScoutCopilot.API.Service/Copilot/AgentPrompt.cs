using System.Reflection;

namespace HomeScoutCopilot.API.Service;

/// <summary>
/// Loads the HomeScout agent's system instructions from a versioned, embedded prompt
/// asset (<c>Prompts/homescout.v{N}.md</c>) instead of a hardcoded string, so the prompt
/// is a first-class, reviewable, diff-able, git-taggable artefact — per Microsoft's
/// GenAIOps prompt-versioning guidance:
/// https://learn.microsoft.com/en-us/training/modules/prompt-versioning-genaiops/4-github-repository-structure
///
/// Bump <see cref="Version"/> (and add the new <c>homescout.vN.md</c> asset) to evolve the
/// prompt; tag the deploy that ships it so the git tag ↔ prompt version ↔ behaviour line up.
/// </summary>
public static class AgentPrompt
{
    /// <summary>The current prompt asset version. Bump when the instructions change.</summary>
    public const string Version = "v2";

    private static readonly string ResourceName =
        $"HomeScoutCopilot.API.Service.Prompts.homescout.{Version}.md";

    /// <summary>The HomeScout system instructions, loaded once from the embedded asset.</summary>
    public static string Instructions { get; } = Load();

    private static string Load()
    {
        var assembly = typeof(AgentPrompt).GetTypeInfo().Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded prompt asset '{ResourceName}' not found. " +
                "Confirm the file exists and is included as an <EmbeddedResource> in the project.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Trim();
    }
}
