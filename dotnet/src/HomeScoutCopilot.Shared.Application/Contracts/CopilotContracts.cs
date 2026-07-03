namespace HomeScoutCopilot.Shared.Application.Contracts;

/// <summary>A natural-language question for the HomeScout copilot.</summary>
public record CopilotRequest(string Message);

/// <summary>A tool the agent invoked while answering — part of the evidence trail.</summary>
public record CopilotToolCall(string Name, string Summary);

/// <summary>
/// The copilot's grounded answer: the prose, the tools it called (evidence trail),
/// the assumptions behind any figures, and the safety caveats.
/// </summary>
public record CopilotAnswer(
    string Text,
    IReadOnlyList<CopilotToolCall> ToolCalls,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Caveats);
