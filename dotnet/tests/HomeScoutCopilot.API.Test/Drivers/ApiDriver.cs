using HomeScoutCopilot.API.Client;
using HomeScoutCopilot.Shared.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HomeScoutCopilot.API.Test.Drivers;

/// <summary>
/// Boots the API in-memory and exercises it through the real typed client, so BDD
/// scenarios hit the same contract as production callers. Reqnroll resolves one
/// instance per scenario and disposes it via <see cref="IDisposable"/>.
/// </summary>
public sealed class ApiDriver : IDisposable
{
    private readonly WebApplicationFactory<HomeScoutCopilot.API.ApiMarker> _factory = new();
    private readonly HomeScoutApiClient _client;

    public ApiDriver() => _client = new HomeScoutApiClient(_factory.CreateClient());

    public HomeScoutStatus? Status { get; private set; }

    public MortgageEstimateResult? Estimate { get; private set; }

    public async Task FetchStatusAsync() => Status = await _client.GetStatusAsync();

    public async Task EstimateMortgageAsync(MortgageEstimateRequest request)
        => Estimate = await _client.EstimateMortgageAsync(request);

    public void Dispose() => _factory.Dispose();
}
