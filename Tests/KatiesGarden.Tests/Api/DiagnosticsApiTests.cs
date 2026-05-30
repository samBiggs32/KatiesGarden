using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace KatiesGarden.Tests.Api;

[Trait("Category", "Integration")]
[Collection("AspireApi")]
public class DiagnosticsApiTests(AspireApiFixture fixture)
{
    [Fact]
    public async Task Get_Diagnostics_DbReachable_ReturnsReady()
    {
        var response = await fixture.HttpClient.GetAsync("/api/diagnostics");

        response.IsSuccessStatusCode.Should().BeTrue();
        var checks = await ReadChecks(response);

        // The real Aspire-managed Postgres is live, so the DB check must pass.
        checks.GetProperty("database").GetString().Should().Be("ok");
    }

    [Fact]
    public async Task Get_Diagnostics_UnconfiguredIntegrations_ReportNotConfigured_NotFail()
    {
        // The AppHost injects placeholders for Stripe and leaves Brevo/Blob unset
        // in the test environment. These MUST surface as "not_configured" (not a
        // failure) so the stack stays healthy locally without real accounts —
        // and so none of these checks ever spends quota.
        var response = await fixture.HttpClient.GetAsync("/api/diagnostics");
        var checks = await ReadChecks(response);

        checks.GetProperty("brevo_api").GetString().Should().Be("not_configured");
        checks.GetProperty("stripe").GetString().Should().Be("not_configured");
        checks.GetProperty("blob_storage").GetString().Should().Be("not_configured");
    }

    [Fact]
    public async Task Get_Diagnostics_DefaultsToReady_WithSmtpSkipped()
    {
        var response = await fixture.HttpClient.GetAsync("/api/diagnostics");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.GetProperty("status").GetString().Should().Be("ready");
        // SMTP is skipped by default — never tested unless explicitly requested,
        // and even then it only connects + authenticates (never sends).
        doc.RootElement.GetProperty("checks").GetProperty("smtp").GetString()
            .Should().Be("skipped");
    }

    [Fact]
    public async Task Get_Diagnostics_WithCheckSmtp_ActuallyRunsTheCheck()
    {
        // ?checkSmtp=true must move SMTP out of "skipped" — proving the opt-in
        // connectivity probe runs. We don't assert reachability (the test host
        // has no real SMTP server), only that it is no longer skipped. The probe
        // connects + authenticates + disconnects; it never sends a message.
        var response = await fixture.HttpClient.GetAsync("/api/diagnostics?checkSmtp=true");
        var checks = await ReadChecks(response);

        checks.GetProperty("smtp").GetString().Should().NotBe("skipped");
    }

    private static async Task<JsonElement> ReadChecks(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("checks").Clone();
    }
}
