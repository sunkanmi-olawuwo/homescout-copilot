using System.Text.Json;
using Azure.AI.Projects;
using Azure.Core;
using HomeScoutCopilot.Shared.Contracts;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace HomeScoutCopilot.API.Service;

/// <summary>
/// The real copilot: a Foundry-backed agent (Microsoft Agent Framework, Responses path).
/// The model decides which of the HomeScout tools to call; the framework runs the
/// tool-call loop and invokes our deterministic services. Verified live via
/// FoundryAgentGatewayLiveTests against a provisioned Foundry project.
/// </summary>
public sealed class FoundryAgentGateway : IHomeScoutAgentGateway
{
    private const string AgentName = "HomeScout";

    // The system prompt lives in a versioned, embedded asset (Prompts/homescout.v1.md),
    // loaded via AgentPrompt — not a hardcoded string. See AgentPrompt for the rationale.
    private static readonly IReadOnlyList<string> Caveats =
    [
        "This is an estimate, not mortgage advice — speak to a qualified mortgage adviser.",
    ];

    private readonly AIAgent _agent;

    public FoundryAgentGateway(IOptions<FoundryOptions> options, TokenCredential credential, HomeScoutAgentTools tools)
    {
        var settings = options.Value;
        _agent = new AIProjectClient(new Uri(settings.ProjectEndpoint), credential)
            .AsAIAgent(
                model: settings.ModelDeploymentName,
                name: AgentName,
                instructions: AgentPrompt.Instructions,
                tools: tools.Build().ToList());
    }

    public async Task<CopilotAnswer> AskAsync(CopilotRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _agent.RunAsync(request.Message, cancellationToken: cancellationToken);

        var contents = response.Messages.SelectMany(message => message.Contents).ToList();

        var toolCalls = contents
            .OfType<FunctionCallContent>()
            .Select(call => new CopilotToolCall(call.Name, "called"))
            .ToList();

        // Match each function result back to the name of the call that produced it, then map
        // the result payload into the structured evidence trail.
        var callNames = contents
            .OfType<FunctionCallContent>()
            .GroupBy(call => call.CallId)
            .ToDictionary(group => group.Key, group => group.First().Name);

        var evidence = new List<EvidenceItem>();
        foreach (var result in contents.OfType<FunctionResultContent>())
        {
            if (result.Result is not null && callNames.TryGetValue(result.CallId, out var name))
            {
                var element = JsonSerializer.SerializeToElement(result.Result);
                evidence.AddRange(CopilotEvidenceBuilder.FromToolResult(name, element));
            }
        }

        return new CopilotAnswer(response.Text, toolCalls, evidence, [], Caveats);
    }
}
