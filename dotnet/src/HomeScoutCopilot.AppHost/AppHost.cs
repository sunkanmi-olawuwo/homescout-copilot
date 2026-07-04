var builder = DistributedApplication.CreateBuilder(args);

// Durable conversation-session store. A data volume keeps history across container restarts in dev;
// the API rehydrates sessions from here so multi-turn context survives an API restart. The API also
// runs without it (NullSessionStore) when no "sessions" connection string is present.
var sessionsDb = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("sessions");

var apiService = builder.AddProject<Projects.HomeScoutCopilot_API>("apiservice")
    .WithReference(sessionsDb)
    .WaitFor(sessionsDb)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

var webfrontend = builder.AddViteApp("webfrontend", "../../../frontend")
    .WithPnpm()
    .WithReference(apiService)
    .WaitFor(apiService);

apiService.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
