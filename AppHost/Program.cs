var builder = DistributedApplication.CreateBuilder(args);

// Local Postgres backing both the API and any ad-hoc psql exploration.
// WithDataVolume persists data across `aspire run` restarts so seeded products
// stick around. WithPgWeb adds a browser-based SQL UI on the Aspire dashboard.
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("katiesgarden-pgdata")
    .WithPgWeb();

var db = postgres.AddDatabase("katiesgardendb");

// Azure Functions isolated worker — Aspire spawns `func start` and exposes its
// logs/traces on the dashboard. DATABASE_URL is the env var Program.cs reads.
var api = builder.AddAzureFunctionsProject<Projects.KatiesGarden_Api>("api")
    .WithReference(db)
    .WithEnvironment("DATABASE_URL", db)
    .WaitFor(db);

// Blazor WebAssembly dev server. Aspire injects the API endpoint as a service
// discovery variable; the client uses HttpClient.BaseAddress in dev anyway,
// but the reference also makes the dashboard show the dependency.
builder.AddProject<Projects.KatiesGarden_Web_Client>("web")
    .WithReference(api);

builder.Build().Run();
