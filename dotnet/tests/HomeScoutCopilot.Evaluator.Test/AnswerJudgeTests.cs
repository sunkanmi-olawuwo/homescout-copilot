namespace HomeScoutCopilot.Evaluator.Test;

// Offline tests for the LLM-judge rubric: the prompt-building + score-parsing (no model call).
[TestFixture]
public class AnswerJudgeTests
{
    [Test]
    public void Parse_reads_a_plain_json_score()
    {
        var score = AnswerJudge.Parse("""{"relevance":5,"usefulness":4,"groundedness":5,"rationale":"clear and grounded"}""");

        Assert.That(score, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(score!.Relevance, Is.EqualTo(5));
            Assert.That(score.Usefulness, Is.EqualTo(4));
            Assert.That(score.Groundedness, Is.EqualTo(5));
            Assert.That(score.Rationale, Does.Contain("grounded"));
            Assert.That(score.Passed(), Is.True);
            Assert.That(score.Average, Is.EqualTo((5 + 4 + 5) / 3.0).Within(0.001));
        });
    }

    [Test]
    public void Parse_tolerates_code_fences_and_surrounding_prose()
    {
        var reply = "Here is my assessment:\n```json\n{\"relevance\": 4, \"usefulness\": 3, \"groundedness\": 4, \"rationale\": \"ok\"}\n```\n";

        var score = AnswerJudge.Parse(reply);

        Assert.That(score, Is.Not.Null);
        Assert.That(score!.Relevance, Is.EqualTo(4));
    }

    [Test]
    public void Parse_clamps_out_of_range_scores_into_1_to_5()
    {
        var score = AnswerJudge.Parse("""{"relevance":9,"usefulness":0,"groundedness":3,"rationale":"x"}""");

        Assert.That(score, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(score!.Relevance, Is.EqualTo(5)); // 9 clamped down
            Assert.That(score.Usefulness, Is.EqualTo(1)); // 0 clamped up
            Assert.That(score.Groundedness, Is.EqualTo(3));
            Assert.That(score.Passed(), Is.False); // usefulness 1 < 3
        });
    }

    [Test]
    public void Parse_returns_null_for_malformed_or_missing()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AnswerJudge.Parse("not json at all"), Is.Null);
            Assert.That(AnswerJudge.Parse("""{"relevance":4,"usefulness":4}"""), Is.Null); // missing groundedness
            Assert.That(AnswerJudge.Parse("{ broken"), Is.Null);
        });
    }

    [Test]
    public void BuildInput_includes_the_query_and_answer()
    {
        var input = AnswerJudge.BuildInput("What is the monthly cost?", "About £1,500 — not mortgage advice.");

        Assert.That(input, Does.Contain("What is the monthly cost?").And.Contain("£1,500"));
    }

    [Test]
    public void Report_summarises_averages_and_pass_rate()
    {
        var results = new List<(string, JudgeScore?)>
        {
            ("a", new JudgeScore(5, 4, 5, "good")),
            ("b", new JudgeScore(2, 3, 4, "weak relevance")),
            ("c", null),
        };

        var summary = QualityReport.Summarise(results);

        Assert.Multiple(() =>
        {
            Assert.That(QualityReport.AllPassed(results), Is.False);
            Assert.That(summary, Does.Contain("Relevance    avg 3.5"));
            Assert.That(summary, Does.Contain("Passed (all dimensions ≥ 3): 1/2"));
            Assert.That(summary, Does.Contain("[c] (no score)"));
        });
    }
}
