namespace HomeScoutCopilot.Shared.Contracts;

/// <summary>Inputs for a rental true-monthly-cost estimate. Rent is required; council tax and bills
/// are optional (null ⇒ excluded from the monthly total and flagged as missing). HomeScout never
/// sources a rent — the rent is the listing's own figure.</summary>
public record RentalCostRequest(
    decimal MonthlyRent,
    decimal? MonthlyCouncilTax = null,
    decimal? EstimatedMonthlyBills = null);

/// <summary>Result of a rental cost estimate. All money values are rounded to 2dp. Deposit figures
/// follow the Tenant Fees Act 2019 (England): tenancy deposit capped at 5 weeks' rent (or 6 weeks
/// when annual rent ≥ £50,000), holding deposit at 1 week.</summary>
public record RentalCostResult(
    decimal WeeklyRent,
    int DepositWeeks,
    decimal TenancyDeposit,
    decimal HoldingDeposit,
    decimal FirstMonthRent,
    decimal UpfrontCost,
    decimal TotalMonthlyCost,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Caveats);
