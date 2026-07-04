using System.Globalization;
using System.Text.Json;
using HomeScoutCopilot.Shared.Contracts;

namespace HomeScoutCopilot.API.Service;

/// <summary>
/// Maps a tool's result into the copilot's structured evidence trail — every figure tagged
/// (<see cref="FigureKind"/>), sourced, and (for external data) stamped with provenance. Pure
/// and offline-testable: the gateway serialises each tool result to JSON and hands it here, so
/// this needs no live agent. Unknown tools and unparseable/failed results yield no evidence.
/// </summary>
public static class CopilotEvidenceBuilder
{
    private const string EstimateSource = "/api/mortgage/estimate";
    private const string RentalSource = "/api/rental/estimate";

    // Case-insensitive so it doesn't matter whether the result was serialised camelCase (Web)
    // or PascalCase — the tool results carry no enums, so no converter is needed.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Build evidence items from one tool call's result.</summary>
    public static IReadOnlyList<EvidenceItem> FromToolResult(string toolName, JsonElement result) =>
        toolName switch
        {
            HomeScoutAgentTools.EstimateMortgageToolName => FromEstimate(result),
            HomeScoutAgentTools.GetBaseRateToolName => FromBaseRate(result),
            HomeScoutAgentTools.EstimateRentalCostToolName => FromRentalEstimate(result),
            _ => [],
        };

    private static IReadOnlyList<EvidenceItem> FromRentalEstimate(JsonElement result)
    {
        if (result.ValueKind != JsonValueKind.Object || HasProperty(result, "error"))
        {
            return [];
        }

        var estimate = Deserialize<RentalCostResult>(result);
        if (estimate is null || estimate.TotalMonthlyCost <= 0)
        {
            return [];
        }

        // Deterministic API computations — kind Estimate, provenance "Live" (freshly computed).
        return
        [
            new EvidenceItem("Total monthly cost", Money(estimate.TotalMonthlyCost), FigureKind.Estimate, RentalSource, "Live"),
            new EvidenceItem("Upfront cost (first month + deposit)", Money(estimate.UpfrontCost), FigureKind.Estimate, RentalSource, "Live"),
            new EvidenceItem("Tenancy deposit", Money(estimate.TenancyDeposit), FigureKind.Estimate, RentalSource, "Live"),
        ];
    }

    private static IReadOnlyList<EvidenceItem> FromEstimate(JsonElement result)
    {
        // The estimate tool returns { error } when the request is invalid — no evidence then.
        if (result.ValueKind != JsonValueKind.Object || HasProperty(result, "error"))
        {
            return [];
        }

        var estimate = Deserialize<MortgageEstimateResult>(result);
        if (estimate is null || estimate.MonthlyPayment <= 0)
        {
            return [];
        }

        // A deterministic API computation — kind Estimate, provenance "Live" (freshly computed).
        return
        [
            new EvidenceItem("Monthly mortgage payment", Money(estimate.MonthlyPayment), FigureKind.Estimate, EstimateSource, "Live"),
            new EvidenceItem("Loan to value", Percent(estimate.LtvPercent), FigureKind.Estimate, EstimateSource, "Live"),
        ];
    }

    private static IReadOnlyList<EvidenceItem> FromBaseRate(JsonElement result)
    {
        if (result.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        var rate = Deserialize<BaseRate>(result);
        if (rate is null || string.IsNullOrWhiteSpace(rate.Source))
        {
            return [];
        }

        // A real external fetch — kind Fact, with its Live/Cache/Fallback provenance.
        return
        [
            new EvidenceItem("BoE base rate", Percent(rate.RatePercent), FigureKind.Fact, rate.Source, rate.Provenance),
        ];
    }

    private static T? Deserialize<T>(JsonElement element)
    {
        try
        {
            return element.Deserialize<T>(Json);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static bool HasProperty(JsonElement element, string name)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string Money(decimal value) =>
        value.ToString("C0", CultureInfo.GetCultureInfo("en-GB"));

    private static string Percent(decimal value) =>
        $"{value.ToString("0.##", CultureInfo.InvariantCulture)}%";
}
