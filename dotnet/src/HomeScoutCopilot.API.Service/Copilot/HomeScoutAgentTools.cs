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
public sealed class HomeScoutAgentTools(IMortgageCostEstimator estimator, IBaseRateProvider baseRate)
{
    /// <summary>Tool name the agent calls to estimate the monthly mortgage cost.</summary>
    public const string EstimateMortgageToolName = "estimate_mortgage";

    /// <summary>Tool name the agent calls to read the BoE base rate (context only).</summary>
    public const string GetBaseRateToolName = "get_base_rate";

    /// <summary>
    /// The tool names the agent exposes — the single source of truth shared by
    /// <see cref="Build"/> and the deploy tooling (the agent manifest).
    /// </summary>
    public static IReadOnlyList<string> ToolNames { get; } =
        [EstimateMortgageToolName, GetBaseRateToolName];

    public IReadOnlyList<AITool> Build() =>
    [
        AIFunctionFactory.Create(EstimateMortgage, name: EstimateMortgageToolName),
        AIFunctionFactory.Create(GetBaseRate, name: GetBaseRateToolName),
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
}
