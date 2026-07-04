using System.Text.Json;
using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Shared.Contracts;

namespace HomeScoutCopilot.API.Test;

// Offline unit tests for the tool-result -> evidence mapping (no live agent). Locks the seam
// the frontend Evidence panel renders: every figure tagged (FigureKind), sourced, and — for
// external data — stamped with provenance.
[TestFixture]
public class CopilotEvidenceBuilderTests
{
    private static JsonElement Json(string json) => JsonSerializer.Deserialize<JsonElement>(json);

    [Test]
    public void Estimate_result_maps_to_tagged_estimate_evidence()
    {
        var result = Json("""
            {"loan":372500,"ltvPercent":80.1,"monthlyPayment":2199.36,"totalRepayment":659806.72,
             "totalInterest":287306.72,"stressTest":{"ratePercent":8.1,"monthlyPayment":2899.74},
             "assumptions":[],"caveats":[]}
            """);

        var evidence = CopilotEvidenceBuilder.FromToolResult("estimate_mortgage", result);

        Assert.Multiple(() =>
        {
            Assert.That(evidence, Has.Count.EqualTo(2));
            var payment = evidence.First(e => e.Label == "Monthly mortgage payment");
            Assert.That(payment.Kind, Is.EqualTo(FigureKind.Estimate));
            Assert.That(payment.Value, Does.Contain("2,199"));
            Assert.That(payment.Source, Is.EqualTo("/api/mortgage/estimate"));
            Assert.That(payment.Provenance, Is.EqualTo("Live"));
            Assert.That(evidence.Any(e => e.Label == "Loan to value" && e.Value.Contains("80.1")), Is.True);
        });
    }

    [Test]
    public void Base_rate_result_maps_to_a_fact_with_its_provenance()
    {
        var result = Json("""
            {"ratePercent":3.75,"effectiveDate":"2026-07-02","provenance":"Cache",
             "source":"Bank of England","note":"Context only."}
            """);

        var evidence = CopilotEvidenceBuilder.FromToolResult("get_base_rate", result);

        Assert.That(evidence, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(evidence[0].Label, Is.EqualTo("BoE base rate"));
            Assert.That(evidence[0].Kind, Is.EqualTo(FigureKind.Fact));
            Assert.That(evidence[0].Value, Does.Contain("3.75"));
            Assert.That(evidence[0].Source, Is.EqualTo("Bank of England"));
            Assert.That(evidence[0].Provenance, Is.EqualTo("Cache"));
        });
    }

    [Test]
    public void Pascal_cased_result_still_maps_case_insensitively()
    {
        var result = Json("""{"MonthlyPayment":2199.36,"LtvPercent":80.1,"Loan":372500,"TotalInterest":1}""");

        var evidence = CopilotEvidenceBuilder.FromToolResult("estimate_mortgage", result);

        Assert.That(evidence, Has.Count.EqualTo(2));
    }

    [Test]
    public void Estimator_error_result_yields_no_evidence()
    {
        var result = Json("""{"error":"Deposit cannot exceed the property price."}""");

        Assert.That(CopilotEvidenceBuilder.FromToolResult("estimate_mortgage", result), Is.Empty);
    }

    [Test]
    public void Rental_estimate_result_maps_to_tagged_estimate_evidence()
    {
        var result = Json("""
            {"weeklyRent":346.15,"depositWeeks":5,"tenancyDeposit":1730.77,"holdingDeposit":346.15,
             "firstMonthRent":1500,"upfrontCost":3230.77,"totalMonthlyCost":1850,
             "assumptions":[],"caveats":[]}
            """);

        var evidence = CopilotEvidenceBuilder.FromToolResult("estimate_rental_cost", result);

        Assert.Multiple(() =>
        {
            Assert.That(evidence, Has.Count.EqualTo(3));
            var monthly = evidence.First(e => e.Label == "Total monthly cost");
            Assert.That(monthly.Kind, Is.EqualTo(FigureKind.Estimate));
            Assert.That(monthly.Value, Does.Contain("1,850"));
            Assert.That(monthly.Source, Is.EqualTo("/api/rental/estimate"));
            Assert.That(monthly.Provenance, Is.EqualTo("Live"));
            // Money formats to whole pounds (C0): £1,730.77 → "£1,731".
            Assert.That(evidence.Any(e => e.Label == "Tenancy deposit" && e.Value.Contains("1,731")), Is.True);
            Assert.That(evidence.Any(e => e.Label.StartsWith("Upfront cost")), Is.True);
        });
    }

    [Test]
    public void Rental_error_result_yields_no_evidence()
    {
        var result = Json("""{"error":"Monthly rent must be greater than zero."}""");

        Assert.That(CopilotEvidenceBuilder.FromToolResult("estimate_rental_cost", result), Is.Empty);
    }

    [Test]
    public void Unknown_tool_yields_no_evidence()
    {
        var result = Json("""{"anything":1}""");

        Assert.That(CopilotEvidenceBuilder.FromToolResult("crime_lookup", result), Is.Empty);
    }
}
