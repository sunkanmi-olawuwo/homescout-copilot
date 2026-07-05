using System.Text.Json.Serialization;
using Azure.AI.Projects;
using Azure.Core;
using Azure.Identity;
using Carter;
using HomeScoutCopilot.API.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Npgsql;

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
builder.Services.AddSingleton<IRentalCostEstimator, RentalCostEstimator>();

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

// End-user authentication (Keycloak/OIDC). Anonymous-capable: the copilot works without login, so
// only the per-user endpoints RequireAuthorization. The Keycloak JWT scheme is wired only when the
// "keycloak" service reference is present (Aspire injects it); the API still runs standalone without
// it (no default scheme → UseAuthentication is a no-op, and the anonymous copilot is unaffected).
// Tokens are validated against the homescout realm + the homescout-api audience.
var keycloakConfigured = builder.Configuration.GetSection("services:keycloak").GetChildren().Any();
if (keycloakConfigured)
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddKeycloakJwtBearer("keycloak", realm: "homescout", options =>
        {
            options.Audience = "homescout-api";
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            // JIT user capture: on every validated token, best-effort record the user so per-user
            // history works. Throttled per subject and never fails auth if the DB is unavailable.
            options.Events = new JwtBearerEvents { OnTokenValidated = RecordAuthenticatedUserAsync };
        });
}
else
{
    // No Keycloak configured (standalone run / tests): register authentication with no default
    // scheme so UseAuthentication stays inert; tests supply their own scheme for authorized routes.
    builder.Services.AddAuthentication();
}

builder.Services.AddAuthorization();

// Resolves the authenticated caller to HomeScout's internal user id (cached). Always registered —
// it delegates to IUserDirectory, returning null when that's the no-op directory.
builder.Services.AddSingleton<IUserResolver, UserResolver>();

// Anonymous multi-turn conversation sessions: keyed by the hs_session cookie, held in memory,
// swept for idle/absolute expiry. Registered unconditionally so the reset endpoint always resolves.
builder.Services.AddOptions<ConversationOptions>().BindConfiguration(ConversationOptions.SectionName);
builder.Services.AddSingleton<ConversationSessionRegistry>();
builder.Services.AddHostedService<ConversationSessionSweeper>();

// Durable session store (PostgreSQL). Registered only when the Aspire-injected "sessions"
// connection string is present; otherwise sessions live only in memory (NullSessionStore) and are
// cleared on restart — graceful degradation, mirroring how the copilot itself is optional.
// The session store and the user directory share the same PostgreSQL server (the "sessions"
// connection string). When absent, both fall back to their no-op implementations so the API still
// runs standalone — sessions are in-memory only and users aren't persisted.
var sessionsConnectionString = builder.Configuration.GetConnectionString("sessions");
if (!string.IsNullOrWhiteSpace(sessionsConnectionString))
{
    builder.Services.AddSingleton(_ => new NpgsqlDataSourceBuilder(sessionsConnectionString).Build());
    builder.Services.AddSingleton<PostgresSessionStore>();
    builder.Services.AddSingleton<ISessionStore>(sp => sp.GetRequiredService<PostgresSessionStore>());
    builder.Services.AddHostedService<PostgresSessionStoreInitializer>();

    builder.Services.AddSingleton<PostgresUserDirectory>();
    builder.Services.AddSingleton<IUserDirectory>(sp => sp.GetRequiredService<PostgresUserDirectory>());
    builder.Services.AddHostedService<PostgresUserDirectoryInitializer>();
}
else
{
    builder.Services.AddSingleton<ISessionStore, NullSessionStore>();
    builder.Services.AddSingleton<IUserDirectory, NullUserDirectory>();
}

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapCarter();

app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();

// JIT user capture on token validation: best-effort record of the (keycloak, sub) user so per-user
// history works, throttled per subject (~10 min) to avoid a write on every request. Never throws —
// a directory/DB blip must not break authentication.
static async Task RecordAuthenticatedUserAsync(Microsoft.AspNetCore.Authentication.JwtBearer.TokenValidatedContext context)
{
    var services = context.HttpContext.RequestServices;
    var directory = services.GetRequiredService<IUserDirectory>();
    if (!directory.IsEnabled)
    {
        return;
    }

    var principal = context.Principal;
    var subject = principal?.FindFirst("sub")?.Value
        ?? principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(subject))
    {
        return;
    }

    var cache = services.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
    var throttleKey = $"user-seen:{UserIdentityProviders.Keycloak}:{subject}";
    if (cache.TryGetValue(throttleKey, out _))
    {
        return;
    }

    try
    {
        var email = principal?.FindFirst("email")?.Value;
        var name = principal?.FindFirst("name")?.Value ?? principal?.FindFirst("preferred_username")?.Value;
        await directory.RecordAsync(UserIdentityProviders.Keycloak, subject, email, name, context.HttpContext.RequestAborted);
        cache.Set(throttleKey, true, TimeSpan.FromMinutes(10));
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("UserCapture");
        logger.LogWarning(ex, "JIT user capture failed for subject {Subject}; continuing.", subject);
    }
}
