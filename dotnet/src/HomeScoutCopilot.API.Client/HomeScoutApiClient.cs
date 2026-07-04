using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HomeScoutCopilot.Shared.Contracts;

namespace HomeScoutCopilot.API.Client;

/// <summary>
/// Typed client over the HomeScout API. Consumed by server-to-server callers and by
/// the API test project's BDD driver so tests exercise the same contract as callers.
/// </summary>
public sealed class HomeScoutApiClient(HttpClient httpClient)
{
    // Match the API: enums travel as strings on the wire (e.g. FigureKind "estimate",
    // RepaymentType "Repayment"), so the client must read/write them with the same converter.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public Task<HomeScoutStatus?> GetStatusAsync(CancellationToken cancellationToken = default) =>
        httpClient.GetFromJsonAsync<HomeScoutStatus>("/api/status", Json, cancellationToken);

    public Task<ComparisonSample?> GetComparisonSampleAsync(CancellationToken cancellationToken = default) =>
        httpClient.GetFromJsonAsync<ComparisonSample>("/api/comparison/sample", Json, cancellationToken);

    public Task<BaseRate?> GetBaseRateAsync(CancellationToken cancellationToken = default) =>
        httpClient.GetFromJsonAsync<BaseRate>("/api/mortgage/base-rate", Json, cancellationToken);

    public async Task<MortgageEstimateResult?> EstimateMortgageAsync(
        MortgageEstimateRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/mortgage/estimate", request, Json, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MortgageEstimateResult>(Json, cancellationToken);
    }

    public async Task<CopilotAnswer?> AskCopilotAsync(
        CopilotRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/copilot/ask", request, Json, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CopilotAnswer>(Json, cancellationToken);
    }
}
