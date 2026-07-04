using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Shared.Contracts;

namespace HomeScoutCopilot.API.Test;

// Locks the deterministic rental estimator: Tenant Fees Act 2019 deposit caps (5 vs 6 weeks),
// upfront and true-monthly maths, and FluentResults validation. The renter analogue of
// MortgageCostEstimatorTests.
[TestFixture]
public class RentalCostEstimatorTests
{
    private readonly RentalCostEstimator _estimator = new();

    private static RentalCostRequest Request(
        decimal monthlyRent = 1_500m,
        decimal? councilTax = null,
        decimal? bills = null)
        => new(monthlyRent, councilTax, bills);

    [Test]
    public void Standard_rent_applies_the_five_week_deposit_cap()
    {
        var result = _estimator.Estimate(Request(monthlyRent: 1_500m));

        Assert.That(result.IsSuccess, Is.True);
        var value = result.Value;
        Assert.Multiple(() =>
        {
            // Annual rent £18,000 < £50,000 → 5-week cap. Weekly = 18000/52 = £346.15.
            Assert.That(value.DepositWeeks, Is.EqualTo(5));
            Assert.That(value.WeeklyRent, Is.EqualTo(346.15m));
            Assert.That(value.TenancyDeposit, Is.EqualTo(1_730.77m));
            Assert.That(value.HoldingDeposit, Is.EqualTo(346.15m));
            Assert.That(value.FirstMonthRent, Is.EqualTo(1_500m));
            // Upfront = first month + tenancy deposit.
            Assert.That(value.UpfrontCost, Is.EqualTo(3_230.77m));
        });
    }

    [Test]
    public void High_rent_applies_the_six_week_deposit_cap()
    {
        var result = _estimator.Estimate(Request(monthlyRent: 5_000m));

        Assert.That(result.IsSuccess, Is.True);
        var value = result.Value;
        Assert.Multiple(() =>
        {
            // Annual rent £60,000 ≥ £50,000 → 6-week cap.
            Assert.That(value.DepositWeeks, Is.EqualTo(6));
            // Weekly = 60000/52 = £1,153.846… → deposit = ×6 = £6,923.08.
            Assert.That(value.TenancyDeposit, Is.EqualTo(6_923.08m));
        });
    }

    [Test]
    public void Deposit_cap_switches_at_the_fifty_thousand_annual_boundary()
    {
        // £4,000/month → £48,000/yr → still 5 weeks.
        Assert.That(_estimator.Estimate(Request(monthlyRent: 4_000m)).Value.DepositWeeks, Is.EqualTo(5));
        // £4,200/month → £50,400/yr → 6 weeks.
        Assert.That(_estimator.Estimate(Request(monthlyRent: 4_200m)).Value.DepositWeeks, Is.EqualTo(6));
    }

    [Test]
    public void Council_tax_and_bills_roll_into_the_true_monthly_cost()
    {
        var result = _estimator.Estimate(Request(monthlyRent: 1_500m, councilTax: 150m, bills: 200m));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.TotalMonthlyCost, Is.EqualTo(1_850m));
    }

    [Test]
    public void Rent_only_true_monthly_cost_equals_the_rent()
    {
        var result = _estimator.Estimate(Request(monthlyRent: 1_500m));

        Assert.That(result.Value.TotalMonthlyCost, Is.EqualTo(1_500m));
    }

    [Test]
    public void Estimate_always_carries_the_not_tenancy_advice_caveat()
    {
        var result = _estimator.Estimate(Request());

        Assert.That(
            result.Value.Caveats.Any(c => c.Contains("not letting or tenancy advice", StringComparison.OrdinalIgnoreCase)),
            Is.True);
    }

    [TestCase(0)]
    [TestCase(-100)]
    public void Non_positive_rent_fails(decimal monthlyRent)
    {
        var result = _estimator.Estimate(Request(monthlyRent: monthlyRent));

        Assert.That(result.IsFailed, Is.True);
        Assert.That(result.Errors.Any(e => e.Message.Contains("greater than zero")), Is.True);
    }

    [Test]
    public void Implausibly_large_rent_fails()
    {
        var result = _estimator.Estimate(Request(monthlyRent: 2_000_000m));

        Assert.That(result.IsFailed, Is.True);
        Assert.That(result.Errors.Any(e => e.Message.Contains("implausibly large")), Is.True);
    }

    [Test]
    public void Negative_council_tax_or_bills_fails()
    {
        Assert.That(_estimator.Estimate(Request(councilTax: -1m)).IsFailed, Is.True);
        Assert.That(_estimator.Estimate(Request(bills: -1m)).IsFailed, Is.True);
    }
}
