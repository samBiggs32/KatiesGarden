using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using KatiesGarden.Api.Data;
using KatiesGarden.Models.Auth;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Xunit;

namespace KatiesGarden.Tests.Api;

/// <summary>
/// Boots the full Aspire stack (Postgres + Functions API + Blazor web) once per
/// test assembly and exposes HttpClient + DbContext to every test that joins the
/// "AspireApi" collection.
///
/// Used by all API integration tests. Test classes share the same Postgres data,
/// so each test uses unique slugs/IDs to stay isolated.
/// </summary>
public sealed class AspireApiFixture : IAsyncLifetime
{
    private DistributedApplication? _app;
    private string? _connectionString;

    public HttpClient HttpClient { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.KatiesGarden_AppHost>();
        _app = await builder.BuildAsync();
        await _app.StartAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("postgres", cts.Token);
        await _app.ResourceNotifications.WaitForResourceAsync(
            "api", KnownResourceStates.Running, cts.Token);

        HttpClient = _app.CreateHttpClient("api");
        HttpClient.Timeout = TimeSpan.FromSeconds(30);

        _connectionString = await _app.GetConnectionStringAsync("katiesgardendb", cts.Token);

        // Wait for the API to actually be responsive — /api/health is the
        // cheapest "ready to take traffic" signal.
        await WaitForHealth(cts.Token);
    }

    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_connectionString)
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>
    /// Returns an HttpClient that includes a base64-encoded admin
    /// <c>x-ms-client-principal</c> header on every request.
    /// </summary>
    public HttpClient CreateAdminClient()
    {
        var principal = new ClientPrincipal(
            IdentityProvider: "github",
            UserId: "test-admin",
            UserDetails: "admin@katiesgarden.test",
            UserRoles: ["authenticated", "admin"]);

        var json = JsonSerializer.Serialize(principal);
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

        var client = new HttpClient(new AddHeaderHandler("x-ms-client-principal", encoded)
        {
            InnerHandler = new HttpClientHandler()
        })
        {
            BaseAddress = HttpClient.BaseAddress,
            Timeout = TimeSpan.FromSeconds(30)
        };
        return client;
    }

    private async Task WaitForHealth(CancellationToken ct)
    {
        for (var i = 0; i < 30; i++)
        {
            try
            {
                var resp = await HttpClient.GetAsync("/api/health", ct);
                if (resp.IsSuccessStatusCode) return;
            }
            catch { /* keep retrying */ }
            await Task.Delay(1000, ct);
        }
        throw new TimeoutException("API did not become healthy within 30 seconds");
    }

    public async ValueTask DisposeAsync()
    {
        HttpClient?.Dispose();
        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    private sealed class AddHeaderHandler(string name, string value) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Add(name, value);
            return base.SendAsync(request, cancellationToken);
        }
    }
}

[CollectionDefinition("AspireApi")]
public sealed class AspireApiCollection : ICollectionFixture<AspireApiFixture>;
