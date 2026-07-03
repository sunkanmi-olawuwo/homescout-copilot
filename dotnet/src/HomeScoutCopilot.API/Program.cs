using System.Text.Json.Serialization;
using Azure.Core;
using Azure.Identity;
using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Functional;
using HomeScoutCopilot.Shared.Application.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddScoped<IHomeScoutService, HomeScoutService>();
builder.Services.AddSingleton<IMortgageCostEstimator, MortgageCostEstimator>();

// Accept/emit enums (e.g. RepaymentType) as strings for friendlier JSON.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddMemoryCache();
builder.Services.AddOptions<BaseRateOptions>()
    .Bind(builder.Configuration.GetSection(BaseRateOptions.SectionName));
builder.Services.AddHttpClient<IBaseRateProvider, BankOfEnglandBaseRateProvider>(client =>
{
    // A descriptive User-Agent; the BoE endpoint rejects requests without one.
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "HomeScoutCopilot/1.0 (+https://github.com/sunkanmi-olawuwo/homescout-copilot)");
});

// Copilot (Foundry agent). Registered only when a Foundry endpoint is configured — the
// /api/copilot/ask endpoint returns 503 until then. Provisioning writes the endpoint as
// AZURE_FOUNDRY_PROJECT_ENDPOINT; Foundry:* config also works.
var foundryEndpoint = builder.Configuration["Foundry:ProjectEndpoint"]
    ?? builder.Configuration["AZURE_FOUNDRY_PROJECT_ENDPOINT"];
if (!string.IsNullOrWhiteSpace(foundryEndpoint))
{
    builder.Services.AddOptions<FoundryOptions>()
        .Bind(builder.Configuration.GetSection(FoundryOptions.SectionName))
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
    builder.Services.AddScoped<HomeScoutAgentTools>();
    builder.Services.AddScoped<IHomeScoutAgentGateway, FoundryAgentGateway>();
}

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var api = app.MapGroup("/api");

api.MapGet("/status", (IHomeScoutService service) => service.GetStatus().ToHttpResult());

api.MapGet("/comparison/sample", (IHomeScoutService service) => service.GetComparisonSample().ToHttpResult());

// Deterministic mortgage estimate from the buyer's own figures. Invalid input becomes
// a 400 ProblemDetails via the FluentResults mapping — not an exception.
api.MapPost("/mortgage/estimate", (MortgageEstimateRequest request, IMortgageCostEstimator estimator)
    => estimator.Estimate(request).ToHttpResult());

// Base rate is orienting context only (never a mortgage product rate); the provider
// never throws, so this endpoint always returns 200 with a live/cache/fallback value.
api.MapGet("/mortgage/base-rate", async (IBaseRateProvider baseRate, CancellationToken cancellationToken)
    => Results.Ok(await baseRate.GetCurrentAsync(cancellationToken)));

// The copilot: a natural-language question → a grounded answer (the agent calls the
// HomeScout tools). 503 when Foundry isn't configured; 400 on an empty message.
api.MapPost("/copilot/ask", async (CopilotRequest request, HttpContext http, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Message))
    {
        return Results.Problem(title: "A message is required.", statusCode: StatusCodes.Status400BadRequest);
    }

    var gateway = http.RequestServices.GetService<IHomeScoutAgentGateway>();
    if (gateway is null)
    {
        return Results.Problem(
            title: "Copilot is not configured",
            detail: "The Foundry project endpoint is not set. Provision Foundry (azd) and set Foundry:ProjectEndpoint / AZURE_FOUNDRY_PROJECT_ENDPOINT.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Ok(await gateway.AskAsync(request, cancellationToken));
});

app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();
