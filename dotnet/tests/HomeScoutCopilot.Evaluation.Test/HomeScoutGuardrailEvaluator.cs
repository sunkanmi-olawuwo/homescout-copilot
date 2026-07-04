using HomeScoutCopilot.Evaluator;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace HomeScoutCopilot.Evaluation.Test;

/// <summary>
/// A custom <see cref="IEvaluator"/> that surfaces HomeScout's deterministic product guardrails
/// (not mortgage advice / no product recommendation / no safe-unsafe area verdict) inside the
/// standard <see cref="Microsoft.Extensions.AI.Evaluation"/> report — labelled as HomeScout's own,
/// alongside Microsoft's research-validated quality metrics. No model call: it reuses the exact
/// regexes from <see cref="SafetyEvaluators"/> that back the CI gate, so a violation reads the same
/// in the report as it does in the gate.
/// </summary>
public sealed class HomeScoutGuardrailEvaluator : IEvaluator
{
    private static readonly IReadOnlyDictionary<string, string> MetricNames = new Dictionary<string, string>
    {
        [nameof(SafetyEvaluators.NotMortgageAdvice)] = "HomeScout guardrail: not mortgage advice",
        [nameof(SafetyEvaluators.NoMortgageProductRecommendation)] = "HomeScout guardrail: no product recommendation",
        [nameof(SafetyEvaluators.NoAreaSafetyVerdict)] = "HomeScout guardrail: no safe/unsafe area verdict",
    };

    public IReadOnlyCollection<string> EvaluationMetricNames => [.. MetricNames.Values];

    public ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        var text = modelResponse.Text ?? string.Empty;

        var metrics = SafetyEvaluators.All
            .Select(check => check(text))
            .Select(result =>
            {
                var metric = new BooleanMetric(MetricNames[result.Evaluator], value: result.Passed, reason: result.Detail)
                {
                    Interpretation = new EvaluationMetricInterpretation(
                        result.Passed ? EvaluationRating.Exceptional : EvaluationRating.Unacceptable,
                        failed: !result.Passed,
                        reason: result.Detail)
                };
                return (EvaluationMetric)metric;
            })
            .ToList();

        return new ValueTask<EvaluationResult>(new EvaluationResult(metrics));
    }
}
