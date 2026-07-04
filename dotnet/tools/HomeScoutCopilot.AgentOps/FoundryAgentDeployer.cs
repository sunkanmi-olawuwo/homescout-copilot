using System.ClientModel;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Core;
using OpenAI.Responses;

namespace HomeScoutCopilot.AgentOps;

/// <summary>
/// Registers the single-sourced <see cref="AgentDefinition"/> as a persisted, versioned agent in
/// the Foundry project (<c>AgentAdministrationClient.CreateAgentVersion</c>) — the live half of the
/// deploy step. This is what makes the agent a named, versioned asset visible in the Foundry portal
/// (the in-process <c>AsAIAgent</c> path the API serves with is ephemeral and shows nothing).
///
/// The persisted definition carries the model + guardrail instructions. Tool execution stays
/// client-side (the API runs the deterministic HomeScout tools in-process), so tools are not
/// declared on the persisted definition here — see the deploy notes in the GenAIOps tooling plan.
/// </summary>
public sealed class FoundryAgentDeployer
{
    private readonly AIProjectClient _projectClient;

    public FoundryAgentDeployer(string projectEndpoint, TokenCredential credential) =>
        _projectClient = new AIProjectClient(new Uri(projectEndpoint), credential);

    /// <summary>Creates (or adds a new version to) the persisted agent and returns the new version.</summary>
    public async Task<ProjectsAgentVersion> DeployAsync(
        AgentDefinition definition, CancellationToken cancellationToken = default)
    {
        var declarativeDefinition = new DeclarativeAgentDefinition(definition.Model)
        {
            Instructions = definition.Instructions,
            ReasoningOptions = BuildReasoningOptions(),
        };

        var options = new ProjectsAgentVersionCreationOptions(declarativeDefinition)
        {
            Description = $"HomeScout homebuying copilot (prompt {definition.PromptVersion}).",
        };

        ClientResult<ProjectsAgentVersion> result =
            await _projectClient.AgentAdministrationClient.CreateAgentVersionAsync(
                agentName: definition.Name, options: options, cancellationToken: cancellationToken);

        if (result.GetRawResponse().IsError)
        {
            throw new InvalidOperationException(
                $"Failed to register agent '{definition.Name}': {result.GetRawResponse().ReasonPhrase}");
        }

        return result.Value;
    }

    // The chat model is a gpt-5 reasoning model, so tune it with reasoning effort rather than
    // temperature/top-p (which reasoning models reject). Medium balances answer quality against
    // latency/cost for a copilot that reasons about costs and explains its assumptions.
    // OPENAI001: the reasoning-options surface is the only knob for reasoning models and is still
    // marked experimental by the OpenAI SDK; opted into deliberately here (a versioned ops asset,
    // not shipped runtime), scoped to this method.
#pragma warning disable OPENAI001
    private static ResponseReasoningOptions BuildReasoningOptions() =>
        new()
        {
            ReasoningEffortLevel = ResponseReasoningEffortLevel.Medium,
            ReasoningSummaryVerbosity = ResponseReasoningSummaryVerbosity.Auto,
        };
#pragma warning restore OPENAI001
}
