using HomeScoutCopilot.API.Test.Drivers;
using Reqnroll;

namespace HomeScoutCopilot.API.Test.StepDefinitions;

[Binding]
public sealed class StatusStepDefinitions(ApiDriver api)
{
    [When("the client requests the product status")]
    public async Task WhenTheClientRequestsTheProductStatus() => await api.FetchStatusAsync();

    [Then("the API reports it is {string}")]
    public void ThenTheApiReportsItIs(string architecture)
    {
        Assert.That(api.Status, Is.Not.Null);
        Assert.That(api.Status!.Architecture, Is.EqualTo(architecture));
    }

    [Then("the API names the React frontend")]
    public void ThenTheApiNamesTheReactFrontend()
    {
        Assert.That(api.Status, Is.Not.Null);
        Assert.That(api.Status!.Frontend, Is.EqualTo("React"));
    }
}
