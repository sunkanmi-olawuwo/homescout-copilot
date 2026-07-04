using System.Text.Json.Serialization;

namespace HomeScoutCopilot.Shared.Contracts;

/// <summary>A natural-language question for the HomeScout copilot.</summary>
public record CopilotRequest(string Message);

/// <summary>A tool the agent invoked while answering — part of the evidence trail.</summary>
public record CopilotToolCall(string Name, string Summary);

/// <summary>
/// How a figure should be read: a verified <c>Fact</c>, a computed <c>Estimate</c>, a stated
/// <c>Assumption</c>, or a <c>Missing</c> gap. Drives the evidence-panel tag so no number is
/// shown without its epistemic status.
/// </summary>
public enum FigureKind
{
    [JsonStringEnumMemberName("fact")] Fact,
    [JsonStringEnumMemberName("estimate")] Estimate,
    [JsonStringEnumMemberName("assumption")] Assumption,
    [JsonStringEnumMemberName("missing")] Missing,
}

/// <summary>
/// One tagged, sourced figure in the copilot's evidence trail. <see cref="Provenance"/> is the
/// external-source freshness ("Live" / "Cache" / "Fallback") where it applies, or null for a
/// purely computed figure.
/// </summary>
public record EvidenceItem(
    string Label,
    string Value,
    FigureKind Kind,
    string Source,
    string? Provenance);

/// <summary>
/// The copilot's grounded answer: the prose, the tools it called, the structured evidence
/// trail (every figure tagged + sourced), the assumptions behind any figures, and the safety
/// caveats.
/// </summary>
public record CopilotAnswer(
    string Text,
    IReadOnlyList<CopilotToolCall> ToolCalls,
    IReadOnlyList<EvidenceItem> Evidence,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Caveats);
