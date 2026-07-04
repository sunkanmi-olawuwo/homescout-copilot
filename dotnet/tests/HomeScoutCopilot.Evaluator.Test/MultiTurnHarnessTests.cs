using HomeScoutCopilot.Evaluator;

namespace HomeScoutCopilot.Evaluator.Test;

// Offline coverage for the multi-turn harness: the dataset parser and the runner that drives an
// ordered turn sequence against ONE session and checks the final answer carried context. The live
// context-carry itself is proven by FoundryAgentGatewayLiveTests (External); here we lock the
// harness shape without a model.
[TestFixture]
public class MultiTurnHarnessTests
{
    [Test]
    public void Parser_reads_turns_and_skips_comments()
    {
        var lines = new[]
        {
            "# comment",
            "",
            """{"id":"c1","turns":["first","second"],"expectFinalContains":"£1,0"}""",
        };

        var cases = MultiTurnDataset.Parse(lines);

        Assert.That(cases, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(cases[0].Id, Is.EqualTo("c1"));
            Assert.That(cases[0].Turns, Is.EqualTo(new[] { "first", "second" }));
            Assert.That(cases[0].ExpectFinalContains, Is.EqualTo("£1,0"));
        });
    }

    [Test]
    public void Parser_rejects_a_single_turn_case()
    {
        var lines = new[] { """{"id":"bad","turns":["only one"],"expectFinalContains":"x"}""" };

        Assert.That(() => MultiTurnDataset.Parse(lines), Throws.TypeOf<FormatException>());
    }

    [Test]
    public async Task Runner_drives_every_turn_against_one_session_in_order()
    {
        var gateway = new RecordingGateway(_ => RecordingGateway.Answer("ok"));
        var cases = new[] { new MultiTurnCase("c1", ["turn A", "turn B", "turn C"], "ok") };

        await MultiTurnEvaluation.RunAsync(gateway, cases);

        Assert.Multiple(() =>
        {
            Assert.That(gateway.Calls.Select(c => c.Message), Is.EqualTo(new[] { "turn A", "turn B", "turn C" }));
            // All turns share one non-null session id, so context can accumulate.
            var sessionIds = gateway.Calls.Select(c => c.SessionId).Distinct().ToList();
            Assert.That(sessionIds, Has.Count.EqualTo(1));
            Assert.That(sessionIds[0], Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task Different_cases_use_different_sessions()
    {
        var gateway = new RecordingGateway(_ => RecordingGateway.Answer("ok"));
        var cases = new[]
        {
            new MultiTurnCase("c1", ["a", "b"], "ok"),
            new MultiTurnCase("c2", ["a", "b"], "ok"),
        };

        await MultiTurnEvaluation.RunAsync(gateway, cases);

        Assert.That(gateway.Calls.Select(c => c.SessionId).Distinct().Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task Carried_context_is_true_when_the_final_answer_contains_the_expected_figure()
    {
        // The "copilot" only produces the interest-only figure on the second turn — i.e. it carried
        // the first turn's numbers. The runner keys off the FINAL answer.
        var gateway = new RecordingGateway(message =>
            RecordingGateway.Answer(message.Contains("interest-only", StringComparison.OrdinalIgnoreCase)
                ? "On interest-only that's about £1,012.50 a month."
                : "The estimated monthly payment is about £1,500.75."));
        var cases = new[]
        {
            new MultiTurnCase("cost-then-io", ["What's the monthly cost?", "And on interest-only?"], "£1,0"),
        };

        var results = await MultiTurnEvaluation.RunAsync(gateway, cases);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].CarriedContext, Is.True);
        Assert.That(MultiTurnEvaluation.AllCarried(results), Is.True);
    }

    [Test]
    public async Task Carried_context_is_false_when_the_copilot_forgets_and_asks_again()
    {
        // A copilot that lost context would re-ask for the figures instead of estimating.
        var gateway = new RecordingGateway(_ =>
            RecordingGateway.Answer("Tell me the property price, deposit, rate and term and I'll estimate."));
        var cases = new[]
        {
            new MultiTurnCase("cost-then-io", ["What's the monthly cost?", "And on interest-only?"], "£1,0"),
        };

        var results = await MultiTurnEvaluation.RunAsync(gateway, cases);

        Assert.Multiple(() =>
        {
            Assert.That(results[0].CarriedContext, Is.False);
            Assert.That(MultiTurnEvaluation.AllCarried(results), Is.False);
            Assert.That(MultiTurnEvaluation.Summarise(results), Does.Contain("FAIL"));
        });
    }

    // The committed multi-turn dataset must stay well-formed: parses, non-empty, every case has at
    // least two turns and a non-empty expectation.
    [Test]
    public void Committed_multiturn_dataset_is_well_formed()
    {
        var path = Path.Combine(
            FindRepoRoot(),
            "dotnet", "tools", "HomeScoutCopilot.Evaluator", "data", "homescout-multiturn-eval.jsonl");

        Assert.That(File.Exists(path), Is.True, $"multi-turn dataset missing at {path}");

        var cases = MultiTurnDataset.Load(path);

        Assert.That(cases, Is.Not.Empty);
        Assert.Multiple(() =>
        {
            foreach (var scenario in cases)
            {
                Assert.That(scenario.Turns.Count, Is.GreaterThanOrEqualTo(2), $"case '{scenario.Id}' needs ≥2 turns");
                Assert.That(scenario.ExpectFinalContains, Is.Not.Empty, $"case '{scenario.Id}' needs an expectation");
            }
        });
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
