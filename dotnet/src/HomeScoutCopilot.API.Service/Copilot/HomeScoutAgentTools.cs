using System.ComponentModel;
using HomeScoutCopilot.Shared.Contracts;
using Microsoft.Extensions.AI;

namespace HomeScoutCopilot.API.Service;

/// <summary>
/// The tools the copilot can call, as provider-agnostic <see cref="AIFunction"/>s over
/// the already-built, tested services. The Foundry agent (Microsoft Agent Framework)
/// is handed <see cref="Build"/> and runs the tool-call loop; the same functions are
/// invoked directly in offline tests. The rate is always the buyer's input — no tool
/// invents one.
/// </summary>
public sealed class HomeScoutAgentTools(
    IMortgageCostEstimator estimator, IBaseRateProvider baseRate, IRentalCostEstimator rental)
{
    /// <summary>Tool name the agent calls to estimate the monthly mortgage cost.</summary>
    public const string EstimateMortgageToolName = "estimate_mortgage";

    /// <summary>Tool name the agent calls to read the BoE base rate (context only).</summary>
    public const string GetBaseRateToolName = "get_base_rate";

    /// <summary>Tool name the agent calls to estimate the true monthly + upfront cost of renting.</summary>
    public const string EstimateRentalCostToolName = "estimate_rental_cost";

    /// <summary>
    /// The tool names the agent exposes — the single source of truth shared by
    /// <see cref="Build"/> and the deploy tooling (the agent manifest).
    /// </summary>
    public static IReadOnlyList<string> ToolNames { get; } =
        [EstimateMortgageToolName, GetBaseRateToolName, EstimateRentalCostToolName];

    public IReadOnlyList<AITool> Build() =>
    [
        AIFunctionFactory.Create(EstimateMortgage, name: EstimateMortgageToolName),
        AIFunctionFactory.Create(GetBaseRate, name: GetBaseRateToolName),
        AIFunctionFactory.Create(EstimateRentalCost, name: EstimateRentalCostToolName),
    ];

    [Description("Estimate the monthly mortgage cost from the buyer's own figures. This is an estimate, not mortgage advice.")]
    public object EstimateMortgage(
        [Description("Property price in GBP")] decimal propertyPrice,
        [Description("Deposit in GBP")] decimal deposit,
        [Description("Annual interest rate as a percent — the buyer's own figure; never invent one")] decimal annualInterestRatePercent,
        [Description("Mortgage term in years")] int termYears,
        [Description("Repayment or InterestOnly")] RepaymentType repaymentType)
    {
        var result = estimator.Estimate(
            new MortgageEstimateRequest(propertyPrice, deposit, annualInterestRatePercent, termYears, repaymentType));

        return result.IsSuccess
            ? result.Value
            : new { error = string.Join("; ", result.Errors.Select(e => e.Message)) };
    }

    [Description("Get the current Bank of England base rate, for context only. It is not a mortgage product rate.")]
    public Task<BaseRate> GetBaseRate(CancellationToken cancellationToken)
        => baseRate.GetCurrentAsync(cancellationToken);

    [Description("Estimate the true monthly cost and upfront cost of renting from the listing's figures. "
        + "Deposit caps follow the Tenant Fees Act 2019 (England). This is an estimate, not tenancy advice.")]
    public object EstimateRentalCost(
        [Description("Monthly rent in GBP")] decimal monthlyRent,
        [Description("Monthly council tax in GBP, if known — omit if unknown")] decimal? monthlyCouncilTax = null,
        [Description("Estimated monthly bills in GBP (energy, water, broadband), if known — omit if unknown")] decimal? estimatedMonthlyBills = null)
    {
        var result = rental.Estimate(new RentalCostRequest(monthlyRent, monthlyCouncilTax, estimatedMonthlyBills));

        return result.IsSuccess
            ? result.Value
            : new { error = string.Join("; ", result.Errors.Select(e => e.Message)) };
    }
}
