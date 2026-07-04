namespace HomeScoutCopilot.Evaluator;

/// <summary>One scenario in the eval dataset: a user query and a copilot response to score.</summary>
public sealed record EvaluationCase(string Id, string Query, string Response);

/// <summary>
/// A multi-turn scenario: an ordered list of user turns driven against a single session, plus the
/// substring the <em>final</em> answer must contain — the carried-over figure that proves context
/// held across turns (e.g. an interest-only estimate answered from an earlier turn's figures).
/// </summary>
public sealed record MultiTurnCase(string Id, IReadOnlyList<string> Turns, string ExpectFinalContains);

/// <summary>The outcome of driving one <see cref="MultiTurnCase"/> against the live copilot.</summary>
public sealed record MultiTurnResult(string Id, string FinalAnswer, bool CarriedContext);

/// <summary>The outcome of one evaluator against one response.</summary>
public sealed record EvaluatorResult(string Evaluator, bool Passed, string Detail);

/// <summary>All evaluator outcomes for one case.</summary>
public sealed record CaseResult(string Id, IReadOnlyList<EvaluatorResult> Results)
{
    public bool Passed => Results.All(r => r.Passed);
}
