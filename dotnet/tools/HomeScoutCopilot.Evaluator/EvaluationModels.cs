namespace HomeScoutCopilot.Evaluator;

/// <summary>One scenario in the eval dataset: a user query and a copilot response to score.</summary>
public sealed record EvaluationCase(string Id, string Query, string Response);

/// <summary>The outcome of one evaluator against one response.</summary>
public sealed record EvaluatorResult(string Evaluator, bool Passed, string Detail);

/// <summary>All evaluator outcomes for one case.</summary>
public sealed record CaseResult(string Id, IReadOnlyList<EvaluatorResult> Results)
{
    public bool Passed => Results.All(r => r.Passed);
}
