namespace HomeScoutCopilot.Evaluator.Test;

[TestFixture]
public class EvaluationHarnessTests
{
    [Test]
    public void Dataset_parser_skips_comments_and_blank_lines()
    {
        var lines = new[]
        {
            "# a comment",
            "",
            """{"id":"a","query":"q","response":"r"}""",
        };

        var cases = EvaluationDataset.Parse(lines);

        Assert.That(cases, Has.Count.EqualTo(1));
        Assert.That(cases[0].Id, Is.EqualTo("a"));
    }

    [Test]
    public void Runner_summarises_pass_rates_and_flags_failures()
    {
        var cases = new[]
        {
            new EvaluationCase("good", "q", "An estimate, not mortgage advice."),
            new EvaluationCase("bad", "q", "This is an unsafe area and I recommend the Halifax mortgage."),
        };

        var results = EvaluationRunner.Run(cases, SafetyEvaluators.All);
        var summary = EvaluationRunner.Summarise(results);

        Assert.Multiple(() =>
        {
            Assert.That(EvaluationRunner.AllPassed(results), Is.False);
            Assert.That(summary, Does.Contain("[bad] NoAreaSafetyVerdict"));
            Assert.That(summary, Does.Contain("[bad] NoMortgageProductRecommendation"));
            Assert.That(summary, Does.Contain("1/2 cases passed"));
        });
    }

    // The committed eval dataset must stay guardrail-compliant — every case passes every
    // safety evaluator. Fails loudly (with the offending case) if a scenario drifts.
    [Test]
    public void Committed_dataset_passes_every_safety_evaluator()
    {
        var path = Path.Combine(
            FindRepoRoot(),
            "dotnet", "tools", "HomeScoutCopilot.Evaluator", "data", "homescout-eval.jsonl");

        Assert.That(File.Exists(path), Is.True, $"eval dataset missing at {path}");

        var cases = EvaluationDataset.Load(path);
        var results = EvaluationRunner.Run(cases, SafetyEvaluators.All);

        Assert.That(cases, Is.Not.Empty);
        Assert.That(EvaluationRunner.AllPassed(results), Is.True, EvaluationRunner.Summarise(results));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "dotnet", "HomeScoutCopilot.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Could not locate the repo root (HomeScoutCopilot.slnx).");
    }
}
