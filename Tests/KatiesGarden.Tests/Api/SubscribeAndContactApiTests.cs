using FluentAssertions;
using KatiesGarden.Api.Data;
using KatiesGarden.Models.Entities;
using KatiesGarden.Models;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace KatiesGarden.Tests.Api;

[Trait("Category", "Integration")]
[Collection("AspireApi")]
public class SubscribeAndContactApiTests(AspireApiFixture fixture)
{
    // ── /api/subscribe ────────────────────────────────────────────────────

    [Fact]
    public async Task Subscribe_ValidEmail_PersistsSubscriber()
    {
        var email = $"alice-{Guid.NewGuid():N}@example.com";
        var body = new SubscribeRequest(email, "Alice");

        var response = await fixture.HttpClient.PostAsJsonAsync("/api/subscribe", body);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await using var db = fixture.CreateDbContext();
        var saved = await db.Subscribers.AsNoTracking().FirstOrDefaultAsync(s => s.Email == email.ToLowerInvariant());
        saved.Should().NotBeNull();
        saved!.FirstName.Should().Be("Alice");
    }

    [Fact]
    public async Task Subscribe_DuplicateEmail_StillReturnsOk()
    {
        var email = $"dup-{Guid.NewGuid():N}@example.com";
        var body = new SubscribeRequest(email, null);

        var first = await fixture.HttpClient.PostAsJsonAsync("/api/subscribe", body);
        var second = await fixture.HttpClient.PostAsJsonAsync("/api/subscribe", body);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK, "duplicate subscriptions are idempotent");

        await using var db = fixture.CreateDbContext();
        var count = await db.Subscribers.CountAsync(s => s.Email == email.ToLowerInvariant());
        count.Should().Be(1);
    }

    [Fact]
    public async Task Subscribe_InvalidEmail_ReturnsBadRequest()
    {
        var body = new SubscribeRequest("not-an-email", null);

        var response = await fixture.HttpClient.PostAsJsonAsync("/api/subscribe", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Subscribe_MissingBody_ReturnsBadRequest()
    {
        var response = await fixture.HttpClient.PostAsync("/api/subscribe", new StringContent(""));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── /api/contact ──────────────────────────────────────────────────────
    //
    // The happy path sends SMTP, which fails in tests because the SMTP host is a
    // placeholder. We assert that validation runs *before* the SMTP attempt, so
    // bad payloads short-circuit to 400 instead of 500.

    [Fact]
    public async Task Contact_InvalidPayload_ReturnsBadRequestBeforeSmtp()
    {
        var body = new ContactUsForm
        {
            FirstName = "",
            LastName = "",
            EmailAddress = "not-an-email",
            EmailBody = "",
            ContactNumber = "",
            EmailSubject = ""
        };

        var response = await fixture.HttpClient.PostAsJsonAsync("/api/contact", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Contact_MissingBody_ReturnsBadRequest()
    {
        var response = await fixture.HttpClient.PostAsync("/api/contact", new StringContent(""));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
