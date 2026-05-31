using FluentAssertions;
using System.Net;
using System.Text;
using Xunit;

namespace KatiesGarden.Tests.Api;

/// <summary>
/// The webhook's first responsibility is signature validation — anything missing
/// or malformed gets rejected before touching the DB. Happy-path testing
/// requires generating a valid HMAC-SHA256 signature against a known secret,
/// which we do at the unit-test level (see StockDecrementIntegrationTests for
/// the DB-side behaviour). Here we just confirm the gate works.
/// </summary>
[Trait("Category", "Integration")]
[Collection("AspireApi")]
public class StripeWebhookApiTests(AspireApiFixture fixture)
{
    [Fact]
    public async Task Post_NoSignature_ReturnsBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await fixture.HttpClient.PostAsync("/api/webhooks/stripe", content, ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_InvalidSignature_ReturnsBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/stripe")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "t=1234567890,v1=not-a-real-signature");

        var response = await fixture.HttpClient.SendAsync(request, ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
