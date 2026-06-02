using FluentValidation;
using KatiesGarden.Api.Configuration;
using KatiesGarden.Api.Data;
using KatiesGarden.Api.Helpers;
using KatiesGarden.Models;
using KatiesGarden.Models.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Net;
using System.Net.Http.Json;

namespace KatiesGarden.Api.Functions;

public class SubscribeFunction(
    AppDbContext db,
    IHttpClientFactory http,
    IValidator<SubscribeRequest> validator,
    IOptions<BrevoOptions> brevoOptions,
    ILogger<SubscribeFunction> logger)
{
    [Function("Subscribe")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "subscribe")] HttpRequestData req)
    {
        var ct = req.FunctionContext.CancellationToken;

        SubscribeRequest? request;
        try { request = await req.ReadFromJsonAsync<SubscribeRequest>(); }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to deserialise subscribe request");
            return await Responses.BadRequest(req, "Invalid request body.");
        }

        if (request is null)
            return await Responses.BadRequest(req, "Request body is required.");

        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            logger.LogInformation("Subscribe validation failed: {Errors}",
                string.Join(", ", validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));
            return await Responses.BadRequest(req, validation.Errors.First().ErrorMessage);
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var firstName = request.FirstName?.Trim();

        await SaveToDatabase(email, firstName, ct);
        await AddToBrevo(email, firstName, ct);

        logger.LogInformation("Newsletter subscription: {EmailHash}", LogRedaction.Hash(email));
        return req.CreateResponse(HttpStatusCode.OK);
    }

    private async Task SaveToDatabase(string email, string? firstName, CancellationToken ct)
    {
        db.Subscribers.Add(new Subscriber { Email = email, FirstName = firstName });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Idempotent: detach the conflicting entity so the tracker stays clean.
            db.ChangeTracker.Clear();
            logger.LogInformation("Subscriber {EmailHash} already exists; treated as success", LogRedaction.Hash(email));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save subscriber {EmailHash} to database", LogRedaction.Hash(email));
        }
    }

    private async Task AddToBrevo(string email, string? firstName, CancellationToken ct)
    {
        var brevo = brevoOptions.Value;
        if (!brevo.IsConfigured)
        {
            logger.LogDebug("Brevo not configured — skipping list sync for {EmailHash}", LogRedaction.Hash(email));
            return;
        }

        try
        {
            var client = http.CreateClient();
            client.DefaultRequestHeaders.Add("api-key", brevo.ApiKey);

            // updateEnabled=true: Brevo upserts instead of returning duplicate_parameter.
            var payload = new
            {
                email,
                attributes = new { FIRSTNAME = firstName ?? string.Empty },
                listIds = new[] { brevo.ListId!.Value },
                updateEnabled = true
            };

            var response = await client.PostAsJsonAsync("https://api.brevo.com/v3/contacts", payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Brevo returned {Status} for {EmailHash}: {Body}",
                    (int)response.StatusCode, LogRedaction.Hash(email), body);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add {EmailHash} to Brevo list {ListId}", LogRedaction.Hash(email), brevo.ListId);
        }
    }

    [Function("Unsubscribe")]
    public async Task<HttpResponseData> Unsubscribe(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "subscribe/unsubscribe")] HttpRequestData req)
    {
        var ct = req.FunctionContext.CancellationToken;

        UnsubscribeRequest? request;
        try { request = await req.ReadFromJsonAsync<UnsubscribeRequest>(); }
        catch
        {
            return await Responses.BadRequest(req, "Invalid request body.");
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Email))
            return await Responses.BadRequest(req, "Email is required.");

        var email = request.Email.Trim().ToLowerInvariant();
        var subscriber = await db.Subscribers.FirstOrDefaultAsync(s => s.Email == email, ct);

        if (subscriber is not null)
        {
            // Audit and delete in one transaction — no evidence gap if the delete succeeds.
            db.Subscribers.Remove(subscriber);
            db.AuditLogs.Add(new Models.Entities.AuditLog
            {
                Action = "SubscriberErased",
                EntityType = "Subscriber",
                EntityId = LogRedaction.Hash(email),
                Details = System.Text.Json.JsonSerializer.Serialize(new { reason = "unsubscribe_request" })
            });
            await db.SaveChangesAsync(ct);

            await RemoveFromBrevo(email, ct);
        }

        // Return 200 regardless — don't reveal whether the email was subscribed.
        logger.LogInformation("Unsubscribe request processed for {EmailHash}", LogRedaction.Hash(email));
        return req.CreateResponse(System.Net.HttpStatusCode.OK);
    }

    private async Task RemoveFromBrevo(string email, CancellationToken ct)
    {
        var brevo = brevoOptions.Value;
        if (!brevo.IsConfigured) return;

        try
        {
            var client = http.CreateClient();
            client.DefaultRequestHeaders.Add("api-key", brevo.ApiKey);
            await client.DeleteAsync($"https://api.brevo.com/v3/contacts/{Uri.EscapeDataString(email)}", ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to remove {EmailHash} from Brevo", LogRedaction.Hash(email));
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException pg && pg.SqlState == Npgsql.PostgresErrorCodes.UniqueViolation;
}

file record UnsubscribeRequest(string? Email);
