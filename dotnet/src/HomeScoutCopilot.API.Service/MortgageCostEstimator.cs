using FluentResults;
using HomeScoutCopilot.Shared.Application.Contracts;

namespace HomeScoutCopilot.API.Service;

/// <summary>
/// Deterministic, offline mortgage monthly-cost estimator. Pure function of its input;
/// no network, no advice. Uses the standard amortisation formula (the government-backed
/// MaPS/MoneyHelper method). Expected failures are FluentResults, not exceptions.
/// </summary>
public interface IMortgageCostEstimator
{
    Result<MortgageEstimateResult> Estimate(MortgageEstimateRequest request);
}

public sealed class MortgageCostEstimator : IMortgageCostEstimator
{
    private const decimal MaxRatePercent = 25m;
    private const int MinTermYears = 1;
    private const int MaxTermYears = 40;
    private const decimal StressUpliftPercent = 3m;

    private static readonly IReadOnlyList<string> BaseCaveats =
    [
        "This is an estimate, not mortgage advice — speak to a qualified mortgage adviser.",
        "It uses the rate you provided; it is not tied to any specific lender or product.",
    ];

    public Result<MortgageEstimateResult> Estimate(MortgageEstimateRequest request)
    {
        var validation = Validate(request);
        if (validation.IsFailed)
        {
            return validation;
        }

        var loan = request.PropertyPrice - request.Deposit;
        var payments = request.TermYears * 12;

        var monthly = MonthlyPayment(loan, request.AnnualInterestRatePercent, request.TermYears, request.RepaymentType);
        var stressMonthly = MonthlyPayment(loan, request.AnnualInterestRatePercent + StressUpliftPercent, request.TermYears, request.RepaymentType);

        var isRepayment = request.RepaymentType == RepaymentType.Repayment;
        decimal? totalRepayment = isRepayment ? Round(monthly * payments) : null;
        var totalInterest = isRepayment
            ? Round(monthly * payments - loan)
            : Round(loan * MonthlyRate(request.AnnualInterestRatePercent) * payments);

        var result = new MortgageEstimateResult(
            Loan: Round(loan),
            LtvPercent: Round(loan / request.PropertyPrice * 100m),
            MonthlyPayment: Round(monthly),
            TotalRepayment: totalRepayment,
            TotalInterest: totalInterest,
            StressTest: new MortgageStressTest(
                request.AnnualInterestRatePercent + StressUpliftPercent,
                Round(stressMonthly)),
            Assumptions: Assumptions(request),
            Caveats: BaseCaveats);

        return Result.Ok(result);
    }

    private static Result Validate(MortgageEstimateRequest r)
    {
        var errors = new List<string>();

        if (r.PropertyPrice <= 0)
        {
            errors.Add("Property price must be greater than zero.");
        }

        if (r.Deposit < 0)
        {
            errors.Add("Deposit cannot be negative.");
        }
        else if (r.PropertyPrice > 0 && r.Deposit >= r.PropertyPrice)
        {
            errors.Add("Deposit must be less than the property price.");
        }

        if (r.AnnualInterestRatePercent < 0)
        {
            errors.Add("Interest rate cannot be negative.");
        }
        else if (r.AnnualInterestRatePercent > MaxRatePercent)
        {
            errors.Add($"Interest rate must be {MaxRatePercent}% or less.");
        }

        if (r.TermYears is < MinTermYears or > MaxTermYears)
        {
            errors.Add($"Term must be between {MinTermYears} and {MaxTermYears} years.");
        }

        if (!Enum.IsDefined(r.RepaymentType))
        {
            errors.Add("Repayment type is not recognised.");
        }

        return errors.Count == 0 ? Result.Ok() : Result.Fail(errors);
    }

    // Standard amortisation: M = P·i·(1+i)^n / ((1+i)^n − 1); interest-only = P·i; i=0 => P/n.
    // Computed entirely in decimal (no floating point) for deterministic money maths.
    private static decimal MonthlyPayment(decimal loan, decimal annualPercent, int termYears, RepaymentType type)
    {
        var i = MonthlyRate(annualPercent);

        if (type == RepaymentType.InterestOnly)
        {
            return loan * i;
        }

        var n = termYears * 12;
        if (i == 0m)
        {
            return loan / n;
        }

        var growth = Pow(1m + i, n);
        return loan * i * growth / (growth - 1m);
    }

    private static decimal MonthlyRate(decimal annualPercent) => annualPercent / 100m / 12m;

    private static decimal Pow(decimal value, int exponent)
    {
        var result = 1m;
        for (var k = 0; k < exponent; k++)
        {
            result *= value;
        }

        return result;
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static IReadOnlyList<string> Assumptions(MortgageEstimateRequest r) =>
    [
        $"{(r.RepaymentType == RepaymentType.Repayment ? "Repayment" : "Interest-only")} mortgage at {r.AnnualInterestRatePercent}% over {r.TermYears} years.",
        "The interest rate is constant for the whole term.",
        "Payments are monthly and on time.",
        "No fees, taxes, insurance, or other ownership costs are included.",
        "No overpayments or early repayment.",
    ];
}
