using FluentValidation;
using KatiesGarden.Api.Configuration;
using KatiesGarden.Api.Data;
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
    private const string PostgresUniqueViolation = "23505";

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

        logger.LogInformation("Newsletter subscription: {Email}", email);
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
            logger.LogInformation("Subscriber {Email} already exists; treated as success", email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save subscriber {Email} to database", email);
        }
    }

    private async Task AddToBrevo(string email, string? firstName, CancellationToken ct)
    {
        var brevo = brevoOptions.Value;
        if (!brevo.IsConfigured)
        {
            logger.LogDebug("Brevo not configured — skipping list sync for {Email}", email);
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
                logger.LogWarning("Brevo returned {Status} for {Email}: {Body}",
                    (int)response.StatusCode, email, body);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add {Email} to Brevo list {ListId}", email, brevo.ListId);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException pg && pg.SqlState == PostgresUniqueViolation;
}
