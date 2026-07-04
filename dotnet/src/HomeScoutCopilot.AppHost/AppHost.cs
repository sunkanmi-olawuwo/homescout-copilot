var builder = DistributedApplication.CreateBuilder(args);

// Durable conversation-session store. A data volume keeps history across container restarts in dev;
// the API rehydrates sessions from here so multi-turn context survives an API restart. The API also
// runs without it (NullSessionStore) when no "sessions" connection string is present.
var sessionsDb = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("sessions");

// End-user sign-in (Keycloak/OIDC), per the Keycloak auth plan. Keycloak keeps its own storage
// (embedded DB + data volume), separate from the app's Postgres. The realm/clients are imported
// from the committed export so they're reproducible; the API validates tokens against this realm.
var keycloak = builder.AddKeycloak("keycloak")
    .WithDataVolume("homescout-keycloak-data")
    .WithRealmImport("./keycloak");

var apiService = builder.AddProject<Projects.HomeScoutCopilot_API>("apiservice")
    .WithReference(sessionsDb)
    .WaitFor(sessionsDb)
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

var webfrontend = builder.AddViteApp("webfrontend", "../../../frontend")
    .WithPnpm()
    .WithReference(apiService)
    .WaitFor(apiService);

apiService.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
