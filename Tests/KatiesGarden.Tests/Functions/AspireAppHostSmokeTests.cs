using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using FluentAssertions;
using Xunit;

namespace KatiesGarden.Tests.Functions;

/// <summary>
/// Boots the real AppHost in-process and asserts that every resource it declares
/// reaches a healthy state. Catches Aspire wiring breakage (bad project reference,
/// version mismatch between AppHost packages, missing env var injection) before
/// it shows up as a "doesn't work locally" surprise.
///
/// Requires Docker (for Postgres) and Azure Functions Core Tools v4 (`func`) on
/// PATH for the api resource to spawn. CI installs both before running.
/// </summary>
[Trait("Category", "Integration")]
public class AspireAppHostSmokeTests
{
    [Fact]
    public async Task AppHost_StartsAllResources()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.KatiesGarden_AppHost>();

        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));

        // Postgres must come up first; api waits for it via WaitFor
        await app.ResourceNotifications.WaitForResourceHealthyAsync("postgres", cts.Token);
        await app.ResourceNotifications.WaitForResourceAsync(
            "api", KnownResourceStates.Running, cts.Token);
        await app.ResourceNotifications.WaitForResourceAsync(
            "web", KnownResourceStates.Running, cts.Token);

        // Hit the api's health endpoint to confirm DATABASE_URL injection worked
        using var httpClient = app.CreateHttpClient("api");
        var response = await httpClient.GetAsync("/api/health", cts.Token);
        response.IsSuccessStatusCode.Should().BeTrue(
            "api should respond to /api/health if Aspire wired DATABASE_URL correctly");

        await app.StopAsync();
    }
}
