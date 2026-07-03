using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Shared.Application.Contracts;

namespace HomeScoutCopilot.API.Test;

[TestFixture]
public class MortgageCostEstimatorTests
{
    private readonly MortgageCostEstimator _estimator = new();

    private static MortgageEstimateRequest Request(
        decimal price = 300_000m,
        decimal deposit = 30_000m,
        decimal rate = 4.5m,
        int term = 25,
        RepaymentType type = RepaymentType.Repayment)
        => new(price, deposit, rate, term, type);

    [Test]
    public void Repayment_estimate_matches_the_amortisation_formula()
    {
        var result = _estimator.Estimate(Request());

        Assert.That(result.IsSuccess, Is.True);
        var value = result.Value;
        Assert.Multiple(() =>
        {
            Assert.That(value.Loan, Is.EqualTo(270_000m));
            Assert.That(value.LtvPercent, Is.EqualTo(90.00m));
            // £270,000 at 4.5% over 25y ≈ £1,500.75/month (reference calculators).
            Assert.That(value.MonthlyPayment, Is.EqualTo(1500.75m).Within(0.10m));
            Assert.That(value.TotalRepayment, Is.Not.Null);
            Assert.That(value.TotalInterest, Is.GreaterThan(0m));
            Assert.That(value.StressTest.RatePercent, Is.EqualTo(7.5m));
            Assert.That(value.StressTest.MonthlyPayment, Is.GreaterThan(value.MonthlyPayment));
            Assert.That(
                value.Caveats.Any(c => c.Contains("not mortgage advice", StringComparison.OrdinalIgnoreCase)),
                Is.True);
        });
    }

    [Test]
    public void Interest_only_payment_is_principal_times_monthly_rate()
    {
        var result = _estimator.Estimate(Request(type: RepaymentType.InterestOnly));

        Assert.That(result.IsSuccess, Is.True);
        // 270,000 × (4.5 / 100 / 12) = 1,012.50
        Assert.That(result.Value.MonthlyPayment, Is.EqualTo(1012.50m));
        Assert.That(result.Value.TotalRepayment, Is.Null);
    }

    [Test]
    public void Zero_rate_repayment_is_loan_divided_by_months()
    {
        var result = _estimator.Estimate(Request(price: 120_000m, deposit: 0m, rate: 0m, term: 10));

        // 120,000 / 120 = 1,000.00
        Assert.That(result.Value.MonthlyPayment, Is.EqualTo(1000.00m));
        Assert.That(result.Value.TotalInterest, Is.EqualTo(0m));
    }

    [Test]
    public void Higher_rate_yields_a_higher_payment()
    {
        var low = _estimator.Estimate(Request(rate: 3m)).Value.MonthlyPayment;
        var high = _estimator.Estimate(Request(rate: 6m)).Value.MonthlyPayment;

        Assert.That(high, Is.GreaterThan(low));
    }

    [TestCase(0, 30_000, 4.5, 25, "Property price")]
    [TestCase(300_000, 300_000, 4.5, 25, "Deposit must be less")]
    [TestCase(300_000, -1, 4.5, 25, "Deposit cannot be negative")]
    [TestCase(300_000, 30_000, 30, 25, "Interest rate must be")]
    [TestCase(300_000, 30_000, -1, 25, "Interest rate cannot be negative")]
    [TestCase(300_000, 30_000, 4.5, 41, "Term must be between")]
    public void Invalid_input_fails_with_a_message(
        decimal price, decimal deposit, decimal rate, int term, string expectedFragment)
    {
        var result = _estimator.Estimate(
            new MortgageEstimateRequest(price, deposit, rate, term, RepaymentType.Repayment));

        Assert.That(result.IsFailed, Is.True);
        Assert.That(result.Errors.Any(e => e.Message.Contains(expectedFragment)), Is.True);
    }
}
