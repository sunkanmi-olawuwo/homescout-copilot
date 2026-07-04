using System.Text.Json;
using Azure.AI.Projects;
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

    // The chat model is a gpt-5 reasoning model, tuned with reasoning effort rather than
    // temperature/top-p (which reasoning models reject). Medium balances answer quality against
    // latency/cost for a copilot that reasons about costs and explains its assumptions. Uses the
    // portable Microsoft.Extensions.AI reasoning surface (not the experimental OpenAI-specific one).
    private static readonly ChatClientAgentRunOptions RunOptions = new(new ChatOptions
    {
        Reasoning = new ReasoningOptions { Effort = ReasoningEffort.Medium },
    });

    private readonly AIAgent _agent;
    private readonly ConversationSessionRegistry _sessions;
    private readonly ISessionStore _store;

    public FoundryAgentGateway(
        AIProjectClient projectClient,
        IOptions<FoundryOptions> options,
        HomeScoutAgentTools tools,
        ConversationSessionRegistry sessions,
        ISessionStore store)
    {
        // The AIProjectClient is a thread-safe singleton; only the agent (which binds request-scoped
        // tools) is built per request. Construction is local — the network call is in RunAsync.
        _agent = projectClient.AsAIAgent(
            model: options.Value.ModelDeploymentName,
            name: AgentName,
            instructions: AgentPrompt.Instructions,
            tools: tools.Build().ToList());
        _sessions = sessions;
        _store = store;
    }

    public async Task<CopilotAnswer> AskAsync(
        CopilotRequest request, string? sessionId = null, CancellationToken cancellationToken = default)
    {
        // With a session id, run against the multi-turn session (follow-ups keep context); without
        // one, run stateless (single-turn) exactly as before. The in-memory registry is the hot
        // cache; on a miss it is rehydrated from the durable store (surviving an API restart), and
        // the session is written back after the turn so the store stays current.
        AgentSession? session = null;
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            session = await _sessions.GetOrCreateAsync(
                sessionId,
                async () =>
                {
                    var saved = await _store.TryLoadAsync(sessionId, cancellationToken).ConfigureAwait(false);
                    return saved is { } state
                        ? await _agent.DeserializeSessionAsync(state, jsonSerializerOptions: null, cancellationToken).ConfigureAwait(false)
                        : await _agent.CreateSessionAsync(cancellationToken).ConfigureAwait(false);
                });
        }

        var response = session is null
            ? await _agent.RunAsync(request.Message, options: RunOptions, cancellationToken: cancellationToken)
            : await _agent.RunAsync(request.Message, session, RunOptions, cancellationToken);

        // Write-through: persist the updated session so history survives a restart. Skipped when the
        // store is a no-op (durability off) to avoid serializing a session nobody will keep.
        if (session is not null && _store.IsPersistent)
        {
            var serialized = await _agent
                .SerializeSessionAsync(session, jsonSerializerOptions: null, cancellationToken)
                .ConfigureAwait(false);
            await _store.SaveAsync(sessionId!, serialized, cancellationToken).ConfigureAwait(false);
        }

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
