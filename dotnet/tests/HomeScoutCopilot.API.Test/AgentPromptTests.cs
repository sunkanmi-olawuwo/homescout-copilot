using HomeScoutCopilot.API.Service;

namespace HomeScoutCopilot.API.Test;

// Locks that the agent prompt loads from its versioned embedded asset and still carries
// the non-negotiable guardrails (not mortgage advice, use tools for numbers, no safe/unsafe
// labels). This is the behaviour-lock for externalising the prompt out of a hardcoded const.
[TestFixture]
public class AgentPromptTests
{
    [Test]
    public void Instructions_load_from_the_embedded_asset()
    {
        Assert.That(AgentPrompt.Instructions, Is.Not.Empty);
        Assert.That(AgentPrompt.Version, Is.EqualTo("v1"));
    }

    [Test]
    public void Instructions_keep_the_non_negotiable_guardrails()
    {
        var prompt = AgentPrompt.Instructions;

        Assert.Multiple(() =>
        {
            Assert.That(prompt, Does.Contain("not a mortgage adviser"));
            Assert.That(prompt, Does.Contain("not mortgage advice"));
            Assert.That(prompt, Does.Contain("estimate_mortgage"));
            Assert.That(prompt, Does.Contain("get_base_rate"));
            Assert.That(prompt, Does.Contain("safe or unsafe"));
        });
    }
}
