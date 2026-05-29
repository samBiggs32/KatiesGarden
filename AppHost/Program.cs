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
// SMTP/Stripe defaults are placeholders so the API can boot in CI without real
// credentials; emails will fail at send time (unreachable host) which is the
// intended behaviour for local dev and integration tests.
var api = builder.AddAzureFunctionsProject<Projects.KatiesGarden_Api>("api")
    .WithReference(db)
    .WithEnvironment("DATABASE_URL", db)
    .WithEnvironment("SMTP_HOST", builder.Configuration["SMTP_HOST"] ?? "localhost")
    .WithEnvironment("SMTP_PORT", builder.Configuration["SMTP_PORT"] ?? "587")
    .WithEnvironment("SMTP_USERNAME", builder.Configuration["SMTP_USERNAME"] ?? "test")
    .WithEnvironment("SMTP_PASSWORD", builder.Configuration["SMTP_PASSWORD"] ?? "test")
    .WithEnvironment("RECIPIENT_EMAIL", builder.Configuration["RECIPIENT_EMAIL"] ?? "test@example.com")
    .WithEnvironment("STRIPE_SECRET_KEY", builder.Configuration["STRIPE_SECRET_KEY"] ?? "sk_test_placeholder")
    .WithEnvironment("STRIPE_WEBHOOK_SECRET", builder.Configuration["STRIPE_WEBHOOK_SECRET"] ?? "whsec_test_placeholder")
    .WithEnvironment("SITE_URL", builder.Configuration["SITE_URL"] ?? "http://localhost:5158")
    .WithHttpHealthCheck("health")
    .WaitFor(db);

// Blazor WebAssembly dev server. Aspire injects the API endpoint as a service
// discovery variable; the client uses HttpClient.BaseAddress in dev anyway,
// but the reference also makes the dashboard show the dependency.
builder.AddProject<Projects.KatiesGarden_Web_Client>("web")
    .WithReference(api);

builder.Build().Run();
