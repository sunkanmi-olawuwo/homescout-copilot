using Azure.Identity;

namespace HomeScoutCopilot.Evaluator.Test;

// Live model-graded judge: makes a real Foundry call, so [Category("External")] (nightly) +
// [Category("Integration")] (excluded from the fast gate). Skips when Foundry isn't provisioned.
[TestFixture]
[Category("Integration")]
[Category("External")]
public class QualityLiveTests
{
    [Test]
    public async Task Judge_scores_a_good_answer_within_range()
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_ENDPOINT");
        // Prefer the dedicated judge deployment; fall back to the generator model when unset.
        var model = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_JUDGE_DEPLOYMENT")
            ?? Environment.GetEnvironmentVariable("AZURE_FOUNDRY_MODEL_DEPLOYMENT");
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(model))
        {
            Assert.Ignore("Foundry not provisioned (AZURE_FOUNDRY_PROJECT_ENDPOINT / AZURE_FOUNDRY_MODEL_DEPLOYMENT unset).");
        }

        var judge = new FoundryAnswerJudge(endpoint!, model!, new DefaultAzureCredential());

        var score = await judge.JudgeAsync(
            "What would the monthly cost be on a £300,000 flat with a 10% deposit at 4.5% over 25 years?",
            "**Estimated monthly repayment: £1,500.75**\n\n## Assumptions\n- £270,000 loan (LTV 90%), 4.5% over 25 years, repayment.\n\nThis is an estimate, not mortgage advice — speak to a qualified mortgage adviser.");

        Assert.That(score, Is.Not.Null, "the judge should return a parseable score for a well-formed answer");
        Assert.Multiple(() =>
        {
            Assert.That(score!.Relevance, Is.InRange(1, 5));
            Assert.That(score.Usefulness, Is.InRange(1, 5));
            Assert.That(score.Groundedness, Is.InRange(1, 5));
        });
        TestContext.Out.WriteLine($"live judge score: R{score!.Relevance} U{score.Usefulness} G{score.Groundedness} — {score.Rationale}");
    }
}
