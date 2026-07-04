using System.Globalization;
using FluentResults;
using HomeScoutCopilot.Shared.Contracts;

namespace HomeScoutCopilot.API.Service;

/// <summary>
/// Deterministic, offline rental true-monthly-cost estimator — the renter analogue of
/// <see cref="IMortgageCostEstimator"/>. Pure function of its input; no network, no advice. The
/// deposit figures follow the Tenant Fees Act 2019 (England): tenancy deposit ≤ 5 weeks' rent
/// (6 weeks when annual rent ≥ £50,000), holding deposit ≤ 1 week. Expected failures are
/// FluentResults, not exceptions.
/// </summary>
public interface IRentalCostEstimator
{
    Result<RentalCostResult> Estimate(RentalCostRequest request);
}

public sealed class RentalCostEstimator : IRentalCostEstimator
{
    private const decimal MaxMonthlyRent = 1_000_000m;
    private const decimal HigherCapThreshold = 50_000m; // annual rent at/above which the cap is 6 weeks
    private const int WeeksPerYear = 52;
    private const int MonthsPerYear = 12;

    private static readonly IReadOnlyList<string> BaseCaveats =
    [
        "This is an estimate, not letting or tenancy advice — check the tenancy agreement and terms.",
        "Deposit caps follow the Tenant Fees Act 2019 (England); Wales, Scotland and Northern Ireland differ.",
        "Bills are your own estimates, not figures sourced by HomeScout.",
    ];

    public Result<RentalCostResult> Estimate(RentalCostRequest request)
    {
        var validation = Validate(request);
        if (validation.IsFailed)
        {
            return validation;
        }

        var annualRent = request.MonthlyRent * MonthsPerYear;
        var weeklyRent = annualRent / WeeksPerYear;
        var depositWeeks = annualRent >= HigherCapThreshold ? 6 : 5;

        var tenancyDeposit = Round(weeklyRent * depositWeeks);
        var holdingDeposit = Round(weeklyRent);
        var firstMonthRent = Round(request.MonthlyRent);
        var upfrontCost = Round(firstMonthRent + tenancyDeposit);

        var councilTax = request.MonthlyCouncilTax ?? 0m;
        var bills = request.EstimatedMonthlyBills ?? 0m;
        var totalMonthly = Round(request.MonthlyRent + councilTax + bills);

        var result = new RentalCostResult(
            WeeklyRent: Round(weeklyRent),
            DepositWeeks: depositWeeks,
            TenancyDeposit: tenancyDeposit,
            HoldingDeposit: holdingDeposit,
            FirstMonthRent: firstMonthRent,
            UpfrontCost: upfrontCost,
            TotalMonthlyCost: totalMonthly,
            Assumptions: Assumptions(request, depositWeeks, tenancyDeposit, firstMonthRent),
            Caveats: BaseCaveats);

        return Result.Ok(result);
    }

    private static Result Validate(RentalCostRequest r)
    {
        var errors = new List<string>();

        if (r.MonthlyRent <= 0)
        {
            errors.Add("Monthly rent must be greater than zero.");
        }
        else if (r.MonthlyRent > MaxMonthlyRent)
        {
            errors.Add("Monthly rent is implausibly large.");
        }

        if (r.MonthlyCouncilTax is < 0)
        {
            errors.Add("Council tax cannot be negative.");
        }

        if (r.EstimatedMonthlyBills is < 0)
        {
            errors.Add("Estimated bills cannot be negative.");
        }

        return errors.Count == 0 ? Result.Ok() : Result.Fail(errors);
    }

    private static IReadOnlyList<string> Assumptions(
        RentalCostRequest r, int depositWeeks, decimal tenancyDeposit, decimal firstMonthRent)
    {
        var assumptions = new List<string>
        {
            $"Tenancy deposit capped at {depositWeeks} weeks' rent (Tenant Fees Act 2019, England; "
            + $"annual rent {(depositWeeks == 6 ? "£50,000 or more" : "under £50,000")}).",
            "Holding deposit capped at 1 week's rent — usually credited toward the first month or deposit.",
            $"Move-in upfront = first month's rent ({Money(firstMonthRent)}) + tenancy deposit ({Money(tenancyDeposit)}).",
            r.MonthlyCouncilTax is { } ct
                ? $"Council tax {Money(ct)}/month included."
                : "Council tax not included — add your band's monthly amount for a true monthly cost.",
            r.EstimatedMonthlyBills is { } bills
                ? $"Estimated bills {Money(bills)}/month included (your estimate)."
                : "Bills not included — add an estimate for a true monthly cost.",
        };

        return assumptions;
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static string Money(decimal value) => value.ToString("C0", CultureInfo.GetCultureInfo("en-GB"));
}
