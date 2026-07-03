namespace HomeScoutCopilot.Shared.Application.Contracts;

public enum RepaymentType
{
    Repayment,
    InterestOnly,
}

/// <summary>Inputs for a mortgage monthly-cost estimate. The rate is the buyer's own
/// figure (from a quote/assumption); HomeScout never sources or recommends a rate.</summary>
public record MortgageEstimateRequest(
    decimal PropertyPrice,
    decimal Deposit,
    decimal AnnualInterestRatePercent,
    int TermYears,
    RepaymentType RepaymentType);

/// <summary>Payment if the rate were higher — a rate-rise stress test.</summary>
public record MortgageStressTest(decimal RatePercent, decimal MonthlyPayment);

/// <summary>Result of a mortgage estimate. All money values are rounded to 2dp.
/// <see cref="TotalRepayment"/> is null for interest-only.</summary>
public record MortgageEstimateResult(
    decimal Loan,
    decimal LtvPercent,
    decimal MonthlyPayment,
    decimal? TotalRepayment,
    decimal TotalInterest,
    MortgageStressTest StressTest,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Caveats);
