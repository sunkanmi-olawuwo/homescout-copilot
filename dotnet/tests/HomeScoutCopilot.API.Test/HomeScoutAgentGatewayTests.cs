using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Shared.Application.Contracts;

namespace HomeScoutCopilot.API.Test;

[TestFixture]
public class HomeScoutAgentGatewayTests
{
    [Test]
    public async Task Fake_gateway_returns_the_configured_answer()
    {
        var expected = new CopilotAnswer(
            "Your estimated monthly repayment is about £1,500.75.",
            [new CopilotToolCall("estimate_mortgage", "£300k, 10% deposit, 4.5%, 25y")],
            ["Rate constant for the term."],
            ["Not mortgage advice."]);
        IHomeScoutAgentGateway gateway = new FakeHomeScoutAgentGateway(_ => expected);

        var answer = await gateway.AskAsync(new CopilotRequest("what would the monthly cost be?"));

        Assert.That(answer, Is.EqualTo(expected));
    }

    [Test]
    public async Task Fake_gateway_default_asks_for_the_figures_with_a_caveat()
    {
        IHomeScoutAgentGateway gateway = new FakeHomeScoutAgentGateway();

        var answer = await gateway.AskAsync(new CopilotRequest("help"));

        Assert.Multiple(() =>
        {
            Assert.That(answer.Text, Does.Contain("estimate the monthly cost"));
            Assert.That(answer.Caveats.Any(c => c.Contains("not mortgage advice", StringComparison.OrdinalIgnoreCase)), Is.True);
        });
    }
}
