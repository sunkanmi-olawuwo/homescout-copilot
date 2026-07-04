using System.Text.Json.Serialization;
using Azure.AI.Projects;
using Azure.Core;
using Azure.Identity;
using Carter;
using HomeScoutCopilot.API.Service;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// Endpoints are Carter modules (Features/) delegating to MediatR handlers.
builder.Services.AddMediatR(config => config.RegisterServicesFromAssemblyContaining<HomeScoutCopilot.API.ApiMarker>());
builder.Services.AddCarter();

// Accept/emit enums (e.g. RepaymentType) as strings for friendlier JSON.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Application services.
builder.Services.AddScoped<IHomeScoutService, HomeScoutService>();
builder.Services.AddSingleton<IMortgageCostEstimator, MortgageCostEstimator>();

builder.Services.AddMemoryCache();
builder.Services.AddValidatedOptions<BaseRateOptions>(builder.Configuration);
builder.Services.AddHttpClient<IBaseRateProvider, BankOfEnglandBaseRateProvider>(client =>
{
    // A descriptive User-Agent; the BoE endpoint rejects requests without one.
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "HomeScoutCopilot/1.0 (+https://github.com/sunkanmi-olawuwo/homescout-copilot)");
});

// Copilot (Foundry agent). Registered only when a Foundry endpoint is configured — the
// /api/copilot/ask endpoint returns 503 until then. azd writes AZURE_FOUNDRY_*; Foundry:* also works.
var foundryEndpoint = builder.Configuration["Foundry:ProjectEndpoint"]
    ?? builder.Configuration["AZURE_FOUNDRY_PROJECT_ENDPOINT"];
if (!string.IsNullOrWhiteSpace(foundryEndpoint))
{
    builder.Services.AddValidatedOptions<FoundryOptions>(builder.Configuration)
        .PostConfigure(options =>
        {
            if (string.IsNullOrWhiteSpace(options.ProjectEndpoint))
            {
                options.ProjectEndpoint = builder.Configuration["AZURE_FOUNDRY_PROJECT_ENDPOINT"] ?? options.ProjectEndpoint;
            }

            var envModel = builder.Configuration["AZURE_FOUNDRY_MODEL_DEPLOYMENT"];
            if (!string.IsNullOrWhiteSpace(envModel))
            {
                options.ModelDeploymentName = envModel;
            }
        });
    builder.Services.AddSingleton<TokenCredential>(new DefaultAzureCredential());
    // The Foundry project client is thread-safe — hold it as a singleton and build only the
    // request-scoped, tool-bound agent per request.
    builder.Services.AddSingleton(sp =>
    {
        var settings = sp.GetRequiredService<IOptions<FoundryOptions>>().Value;
        return new AIProjectClient(new Uri(settings.ProjectEndpoint), sp.GetRequiredService<TokenCredential>());
    });
    builder.Services.AddScoped<HomeScoutAgentTools>();
    builder.Services.AddScoped<IHomeScoutAgentGateway, FoundryAgentGateway>();
}

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapCarter();

app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();
