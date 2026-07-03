using HomeScoutCopilot.API.Test.Drivers;
using HomeScoutCopilot.Shared.Contracts;
using Reqnroll;

namespace HomeScoutCopilot.API.Test.StepDefinitions;

[Binding]
public sealed class MortgageEstimateStepDefinitions(ApiDriver api)
{
    [When("I estimate a repayment mortgage for a {int} property with a {int} deposit at {float}% over {int} years")]
    public async Task WhenIEstimateARepaymentMortgage(int price, int deposit, decimal rate, int termYears)
        => await api.EstimateMortgageAsync(
            new MortgageEstimateRequest(price, deposit, rate, termYears, RepaymentType.Repayment));

    [Then("the estimated monthly payment is about {int}")]
    public void ThenTheEstimatedMonthlyPaymentIsAbout(int expected)
    {
        Assert.That(api.Estimate, Is.Not.Null);
        Assert.That(api.Estimate!.MonthlyPayment, Is.EqualTo((decimal)expected).Within(50m));
    }

    [Then("the estimate is labelled not mortgage advice")]
    public void ThenTheEstimateIsLabelledNotMortgageAdvice()
        => Assert.That(
            api.Estimate!.Caveats.Any(c => c.Contains("not mortgage advice", StringComparison.OrdinalIgnoreCase)),
            Is.True);
}
