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

    private const string Instructions =
        """
        You are HomeScout, a UK homebuying due-diligence copilot — not a mortgage adviser.
        Use the tools for any numbers. Call estimate_mortgage for monthly costs; never
        invent an interest rate — use the buyer's figure or ask for it. Call get_base_rate
        only for context (it is not a mortgage product rate). Always state the assumptions
        behind an estimate, and end with: "This is an estimate, not mortgage advice — speak
        to a qualified mortgage adviser." Never recommend a specific mortgage product, and
        never label an area simply safe or unsafe.
        """;

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
                instructions: Instructions,
                tools: tools.Build().ToList());
    }

    public async Task<CopilotAnswer> AskAsync(CopilotRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _agent.RunAsync(request.Message, cancellationToken: cancellationToken);

        var toolCalls = response.Messages
            .SelectMany(message => message.Contents)
            .OfType<FunctionCallContent>()
            .Select(call => new CopilotToolCall(call.Name, "called"))
            .ToList();

        return new CopilotAnswer(response.Text, toolCalls, [], Caveats);
    }
}
