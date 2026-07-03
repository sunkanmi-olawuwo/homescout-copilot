using System.Net.Http.Json;
using HomeScoutCopilot.Shared.Contracts;

namespace HomeScoutCopilot.API.Client;

/// <summary>
/// Typed client over the HomeScout API. Consumed by server-to-server callers and by
/// the API test project's BDD driver so tests exercise the same contract as callers.
/// </summary>
public sealed class HomeScoutApiClient(HttpClient httpClient)
{
    public Task<HomeScoutStatus?> GetStatusAsync(CancellationToken cancellationToken = default) =>
        httpClient.GetFromJsonAsync<HomeScoutStatus>("/api/status", cancellationToken);

    public Task<ComparisonSample?> GetComparisonSampleAsync(CancellationToken cancellationToken = default) =>
        httpClient.GetFromJsonAsync<ComparisonSample>("/api/comparison/sample", cancellationToken);

    public Task<BaseRate?> GetBaseRateAsync(CancellationToken cancellationToken = default) =>
        httpClient.GetFromJsonAsync<BaseRate>("/api/mortgage/base-rate", cancellationToken);

    public async Task<MortgageEstimateResult?> EstimateMortgageAsync(
        MortgageEstimateRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/mortgage/estimate", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MortgageEstimateResult>(cancellationToken);
    }

    public async Task<CopilotAnswer?> AskCopilotAsync(
        CopilotRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/copilot/ask", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CopilotAnswer>(cancellationToken);
    }
}
