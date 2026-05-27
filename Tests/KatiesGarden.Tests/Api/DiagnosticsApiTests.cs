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
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("status").GetString().Should().Be("ready");
        doc.RootElement.GetProperty("checks").GetProperty("database").GetString().Should().Be("ok");
        // Brevo is not configured in tests — must NOT be reported as fail
        doc.RootElement.GetProperty("checks").GetProperty("brevo_api").GetString()
            .Should().Be("not_configured");
    }
}
