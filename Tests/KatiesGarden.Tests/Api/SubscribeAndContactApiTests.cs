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
        var ct = TestContext.Current.CancellationToken;
        var email = $"alice-{Guid.NewGuid():N}@example.com";
        var body = new SubscribeRequest(email, "Alice");

        var response = await fixture.HttpClient.PostAsJsonAsync("/api/subscribe", body, ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await using var db = fixture.CreateDbContext();
        var saved = await db.Subscribers.AsNoTracking().FirstOrDefaultAsync(s => s.Email == email.ToLowerInvariant(), ct);
        saved.Should().NotBeNull();
        saved!.FirstName.Should().Be("Alice");
    }

    [Fact]
    public async Task Subscribe_DuplicateEmail_StillReturnsOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = $"dup-{Guid.NewGuid():N}@example.com";
        var body = new SubscribeRequest(email, null);

        var first = await fixture.HttpClient.PostAsJsonAsync("/api/subscribe", body, ct);
        var second = await fixture.HttpClient.PostAsJsonAsync("/api/subscribe", body, ct);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK, "duplicate subscriptions are idempotent");

        await using var db = fixture.CreateDbContext();
        var count = await db.Subscribers.CountAsync(s => s.Email == email.ToLowerInvariant(), ct);
        count.Should().Be(1);
    }

    [Fact]
    public async Task Subscribe_InvalidEmail_ReturnsBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var body = new SubscribeRequest("not-an-email", null);

        var response = await fixture.HttpClient.PostAsJsonAsync("/api/subscribe", body, ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Subscribe_MissingBody_ReturnsBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await fixture.HttpClient.PostAsync("/api/subscribe", new StringContent(""), ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── /api/subscribe/unsubscribe ────────────────────────────────────────

    [Fact]
    public async Task Unsubscribe_ExistingEmail_RemovesSubscriberAndWritesAudit()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = $"unsub-{Guid.NewGuid():N}@example.com";

        // Subscribe first
        await fixture.HttpClient.PostAsJsonAsync("/api/subscribe", new SubscribeRequest(email, "Test"), ct);

        var response = await fixture.HttpClient.PostAsJsonAsync(
            "/api/subscribe/unsubscribe", new { email }, ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var db = fixture.CreateDbContext();
        var remaining = await db.Subscribers.AnyAsync(s => s.Email == email.ToLowerInvariant(), ct);
        remaining.Should().BeFalse("subscriber row must be deleted on unsubscribe");

        var audit = await db.AuditLogs.FirstOrDefaultAsync(
            a => a.Action == "SubscriberErased" && a.EntityId != null, ct);
        audit.Should().NotBeNull("erasure must be recorded in audit_logs (GDPR evidence)");
    }

    [Fact]
    public async Task Unsubscribe_UnknownEmail_StillReturnsOk()
    {
        var ct = TestContext.Current.CancellationToken;

        // Should return 200 regardless — must not expose whether the address was subscribed
        var response = await fixture.HttpClient.PostAsJsonAsync(
            "/api/subscribe/unsubscribe", new { email = "nobody@example.com" }, ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Unsubscribe_MissingEmail_ReturnsBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await fixture.HttpClient.PostAsJsonAsync(
            "/api/subscribe/unsubscribe", new { }, ct);

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
        var ct = TestContext.Current.CancellationToken;
        var body = new ContactUsForm
        {
            FirstName = "",
            LastName = "",
            EmailAddress = "not-an-email",
            EmailBody = "",
            ContactNumber = "",
            EmailSubject = ""
        };

        var response = await fixture.HttpClient.PostAsJsonAsync("/api/contact", body, ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Contact_MissingBody_ReturnsBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await fixture.HttpClient.PostAsync("/api/contact", new StringContent(""), ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
