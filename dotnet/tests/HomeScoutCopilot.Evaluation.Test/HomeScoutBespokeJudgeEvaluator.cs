using HomeScoutCopilot.Evaluator;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace HomeScoutCopilot.Evaluation.Test;

/// <summary>
/// A custom <see cref="IEvaluator"/> that runs HomeScout's own bespoke judge rubric
/// (<see cref="AnswerJudge"/>) through the shared judge <see cref="IChatClient"/> and reports its
/// relevance / usefulness / groundedness scores. Kept deliberately alongside Microsoft's built-in
/// evaluators so the report shows both side-by-side — our hand-tuned homebuying rubric vs. the
/// research-validated general metrics. Parsing is delegated to the pure, offline-tested
/// <see cref="AnswerJudge.Parse"/>.
/// </summary>
public sealed class HomeScoutBespokeJudgeEvaluator : IEvaluator
{
    public const string RelevanceMetric = "HomeScout bespoke: relevance";
    public const string UsefulnessMetric = "HomeScout bespoke: usefulness";
    public const string GroundednessMetric = "HomeScout bespoke: groundedness";

    public IReadOnlyCollection<string> EvaluationMetricNames => [RelevanceMetric, UsefulnessMetric, GroundednessMetric];

    public async ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        if (chatConfiguration is null)
        {
            throw new ArgumentNullException(nameof(chatConfiguration), "The bespoke judge needs a judge chat client.");
        }

        var query = messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? string.Empty;
        var answer = modelResponse.Text ?? string.Empty;

        var judgeReply = await chatConfiguration.ChatClient.GetResponseAsync(
            [
                new ChatMessage(ChatRole.System, AnswerJudge.Instructions),
                new ChatMessage(ChatRole.User, AnswerJudge.BuildInput(query, answer)),
            ],
            cancellationToken: cancellationToken);

        var score = AnswerJudge.Parse(judgeReply.Text);

        var result = new EvaluationResult(
            Numeric(RelevanceMetric, score?.Relevance, score?.Rationale),
            Numeric(UsefulnessMetric, score?.Usefulness, score?.Rationale),
            Numeric(GroundednessMetric, score?.Groundedness, score?.Rationale));

        if (score is null)
        {
            result.AddDiagnosticsToAllMetrics(
                EvaluationDiagnostic.Error("The bespoke judge did not return a parseable JSON score."));
        }

        return result;
    }

    private static NumericMetric Numeric(string name, int? value, string? rationale)
    {
        var metric = new NumericMetric(name, value: value, reason: rationale)
        {
            Interpretation = value is null
                ? new EvaluationMetricInterpretation(EvaluationRating.Unknown, failed: true, reason: "No score returned.")
                : new EvaluationMetricInterpretation(
                    value >= 3 ? EvaluationRating.Good : EvaluationRating.Unacceptable,
                    failed: value < 3,
                    reason: rationale ?? $"Score {value}/5.")
        };
        return metric;
    }
}
