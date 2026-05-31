using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using Xunit;

namespace KatiesGarden.Tests.Api;

/// <summary>
/// AdminImageFunction needs an Azure Storage backend. The Aspire test stack
/// doesn't include Azurite, so the BlobServiceClient is null and uploads
/// return 503 ServiceUnavailable. These tests cover the auth gate and
/// the graceful degradation path; the happy-path upload is not covered
/// until Azurite is added to the AppHost.
/// </summary>
[Trait("Category", "Integration")]
[Collection("AspireApi")]
public class AdminImageApiTests(AspireApiFixture fixture)
{
    [Fact]
    public async Task UploadImage_Unauthenticated_Returns401()
    {
        var ct = TestContext.Current.CancellationToken;
        var content = new ByteArrayContent([0x00, 0x01]);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        var response = await fixture.HttpClient.PostAsync("/api/manage/images", content, ct);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UploadImage_Admin_NoStorageConfigured_Returns503()
    {
        var ct = TestContext.Current.CancellationToken;
        using var admin = fixture.CreateAdminClient();
        var content = new ByteArrayContent([0x00, 0x01]);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        var response = await admin.PostAsync("/api/manage/images", content, ct);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task UploadImage_Admin_InvalidContentType_ReturnsBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        using var admin = fixture.CreateAdminClient();
        var content = new ByteArrayContent([0x00, 0x01]);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        var response = await admin.PostAsync("/api/manage/images", content, ct);

        // Content-type check runs BEFORE the storage null-check, so this returns 400
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
