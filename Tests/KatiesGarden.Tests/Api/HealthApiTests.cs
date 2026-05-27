using FluentAssertions;
using Xunit;

namespace KatiesGarden.Tests.Api;

[Trait("Category", "Integration")]
[Collection("AspireApi")]
public class HealthApiTests(AspireApiFixture fixture)
{
    [Fact]
    public async Task Get_Health_ReturnsOk()
    {
        var response = await fixture.HttpClient.GetAsync("/api/health");

        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be("OK");
    }
}
