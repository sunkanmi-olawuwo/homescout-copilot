using System.Text.Json;

namespace HomeScoutCopilot.Evaluator;

/// <summary>A model-graded quality score for one copilot answer (each dimension 1–5).</summary>
public sealed record JudgeScore(int Relevance, int Usefulness, int Groundedness, string Rationale)
{
    public double Average => (Relevance + Usefulness + Groundedness) / 3.0;

    public bool Passed(int threshold = 3) =>
        Relevance >= threshold && Usefulness >= threshold && Groundedness >= threshold;
}

/// <summary>
/// The LLM-as-judge rubric + parsing for grading copilot answers on quality (relevance /
/// usefulness / groundedness) — the model-graded complement to the deterministic safety evals.
/// The prompt-building and score-parsing are pure and offline-testable; the actual model call
/// lives in <see cref="FoundryAnswerJudge"/> (verified live).
/// </summary>
public static class AnswerJudge
{
    public const string Instructions =
        """
        You are a strict evaluator of a UK homebuying copilot's answers. Score the answer on
        three dimensions, each an integer 1–5 (5 = excellent, 1 = poor):
        - relevance: does it directly address the buyer's question?
        - usefulness: is it clear, actionable, and appropriately caveated?
        - groundedness: are the figures and claims consistent with the answer's own stated
          assumptions (no invented or contradictory numbers)?
        Reply with ONLY a JSON object and nothing else:
        {"relevance": n, "usefulness": n, "groundedness": n, "rationale": "one short sentence"}
        """;

    public static string BuildInput(string query, string answer) =>
        $"Question:\n{query}\n\nAnswer:\n{answer}\n\nScore the answer as JSON.";

    /// <summary>Parse the judge's reply into a score, tolerating surrounding prose / code fences.
    /// Returns null when the reply has no valid JSON or a missing dimension.</summary>
    public static JudgeScore? Parse(string judgeReply)
    {
        var json = ExtractJsonObject(judgeReply);
        if (json is null)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var relevance = ReadScore(root, "relevance");
            var usefulness = ReadScore(root, "usefulness");
            var groundedness = ReadScore(root, "groundedness");
            if (relevance < 1 || usefulness < 1 || groundedness < 1)
            {
                return null;
            }

            var rationale = root.TryGetProperty("rationale", out var r) ? r.GetString() ?? string.Empty : string.Empty;
            return new JudgeScore(relevance, usefulness, groundedness, rationale);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static int ReadScore(JsonElement root, string name) =>
        root.TryGetProperty(name, out var element) && element.TryGetInt32(out var value)
            ? Math.Clamp(value, 1, 5)
            : 0;

    private static string? ExtractJsonObject(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start ? text[start..(end + 1)] : null;
    }
}
