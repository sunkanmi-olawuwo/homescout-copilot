using System.Text.Json;
using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Shared.Contracts;
using Microsoft.Extensions.AI;

namespace HomeScoutCopilot.API.Test;

[TestFixture]
public class HomeScoutAgentToolsTests
{
    private static HomeScoutAgentTools Tools()
        => new(
            new MortgageCostEstimator(),
            new StubBaseRateProvider(
                new BaseRate(3.75m, new DateOnly(2026, 6, 19), "Fallback", "Bank of England", "Context only.")),
            new RentalCostEstimator());

    private static AIFunction Function(string name)
        => (AIFunction)Tools().Build().First(t => t.Name == name);

    [Test]
    public void Tools_expose_estimate_and_base_rate()
    {
        var names = Tools().Build().Select(t => t.Name).ToArray();

        Assert.That(names, Does.Contain("estimate_mortgage").And.Contains("get_base_rate"));
    }

    [Test]
    public void ToolNames_stay_in_step_with_the_built_tools()
    {
        // ToolNames is the single source shared with the deploy tooling (the agent manifest);
        // it must match the tools Build() actually exposes, or the manifest drifts from reality.
        var built = Tools().Build().Select(tool => tool.Name).ToArray();

        Assert.That(HomeScoutAgentTools.ToolNames, Is.EquivalentTo(built));
    }

    [Test]
    public async Task Estimate_mortgage_tool_routes_to_the_estimator()
    {
        var function = Function("estimate_mortgage");
        var arguments = new AIFunctionArguments
        {
            ["propertyPrice"] = 300_000m,
            ["deposit"] = 30_000m,
            ["annualInterestRatePercent"] = 4.5m,
            ["termYears"] = 25,
            ["repaymentType"] = RepaymentType.Repayment,
        };

        var result = await function.InvokeAsync(arguments, TestContext.CurrentContext.CancellationToken);

        Assert.That(JsonSerializer.Serialize(result), Does.Contain("1500.75"));
    }

    [Test]
    public async Task Get_base_rate_tool_routes_to_the_provider()
    {
        var function = Function("get_base_rate");

        var result = await function.InvokeAsync(new AIFunctionArguments(), TestContext.CurrentContext.CancellationToken);

        Assert.That(JsonSerializer.Serialize(result), Does.Contain("3.75"));
    }

    [Test]
    public async Task Estimate_rental_cost_tool_routes_to_the_estimator()
    {
        var function = Function("estimate_rental_cost");
        var arguments = new AIFunctionArguments
        {
            ["monthlyRent"] = 1_500m,
            ["monthlyCouncilTax"] = 150m,
            ["estimatedMonthlyBills"] = 200m,
        };

        var result = await function.InvokeAsync(arguments, TestContext.CurrentContext.CancellationToken);

        // True monthly cost £1,850 and the 5-week tenancy deposit £1,730.77.
        var json = JsonSerializer.Serialize(result);
        Assert.That(json, Does.Contain("1850").And.Contain("1730.77"));
    }
}
