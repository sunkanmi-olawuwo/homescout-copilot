using Azure.Core;
using Azure.Identity;
using HomeScoutCopilot.API.Service;
using Microsoft.Extensions.DependencyInjection;

namespace HomeScoutCopilot.Evaluator;

/// <summary>
/// Builds a live Foundry-backed copilot gateway from the <c>AZURE_FOUNDRY_*</c> env, mirroring
/// the API's DI (estimator + BoE base-rate provider + tools + agent). Returns null when Foundry
/// is not configured, so the caller can fall back to the offline dataset check.
/// </summary>
public static class CopilotGatewayFactory
{
    public static ServiceProvider? TryBuild()
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_ENDPOINT");
        var model = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_MODEL_DEPLOYMENT");
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(model))
        {
            return null;
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddSingleton<IMortgageCostEstimator, MortgageCostEstimator>();
        services.AddOptions<BaseRateOptions>();
        services.AddHttpClient<IBaseRateProvider, BankOfEnglandBaseRateProvider>(client =>
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "HomeScoutCopilot/1.0 (+https://github.com/sunkanmi-olawuwo/homescout-copilot)"));
        services.Configure<FoundryOptions>(o =>
        {
            o.ProjectEndpoint = endpoint!;
            o.ModelDeploymentName = model!;
        });
        services.AddSingleton<TokenCredential>(new DefaultAzureCredential());
        services.AddScoped<HomeScoutAgentTools>();
        services.AddScoped<IHomeScoutAgentGateway, FoundryAgentGateway>();

        return services.BuildServiceProvider();
    }
}
