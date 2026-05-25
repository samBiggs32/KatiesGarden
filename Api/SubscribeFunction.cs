using FluentValidation;
using KatiesGarden.Api.Configuration;
using KatiesGarden.Api.Data;
using KatiesGarden.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Net;
using System.Net.Http.Json;

namespace KatiesGarden.Api;

public class SubscribeFunction
{
    private const string PostgresUniqueViolation = "23505";

    private readonly ILogger _logger;
    private readonly IServiceProvider _services;
    private readonly IHttpClientFactory _http;
    private readonly IValidator<SubscribeRequest> _validator;
    private readonly BrevoOptions _brevo;

    public SubscribeFunction(
        ILoggerFactory loggerFactory,
        IServiceProvider services,
        IHttpClientFactory http,
        IValidator<SubscribeRequest> validator,
        IOptions<BrevoOptions> brevoOptions)
    {
        _logger = loggerFactory.CreateLogger<SubscribeFunction>();
        _services = services;
        _http = http;
        _validator = validator;
        _brevo = brevoOptions.Value;
    }

    [Function("Subscribe")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "subscribe")] HttpRequestData req)
    {
        var ct = req.FunctionContext.CancellationToken;

        SubscribeRequest? request;
        try { request = await req.ReadFromJsonAsync<SubscribeRequest>(); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialise subscribe request");
            return await Responses.BadRequest(req, "Invalid request body.");
        }

        if (request is null)
            return await Responses.BadRequest(req, "Request body is required.");

        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            _logger.LogInformation("Subscribe validation failed: {Errors}",
                string.Join(", ", validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));
            return await Responses.BadRequest(req, validation.Errors.First().ErrorMessage);
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var firstName = request.FirstName?.Trim();

        await SaveToDatabase(email, firstName, ct);
        await AddToBrevo(email, firstName, ct);

        _logger.LogInformation("Newsletter subscription: {Email}", email);
        return req.CreateResponse(HttpStatusCode.OK);
    }

    private async Task SaveToDatabase(string email, string? firstName, CancellationToken ct)
    {
        var db = _services.GetService<AppDbContext>();
        if (db is null)
        {
            _logger.LogWarning("AppDbContext not registered — skipping DB write for {Email}", email);
            return;
        }

        db.Subscribers.Add(new Subscriber { Email = email, FirstName = firstName });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Idempotent: another request already inserted this email. Detach so EF
            // doesn't keep the conflicting entity tracked for the rest of the request.
            db.ChangeTracker.Clear();
            _logger.LogInformation("Subscriber {Email} already exists; treated as success", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save subscriber {Email} to database", email);
        }
    }

    private async Task AddToBrevo(string email, string? firstName, CancellationToken ct)
    {
        if (!_brevo.IsConfigured)
        {
            _logger.LogDebug("Brevo not configured — skipping list sync for {Email}", email);
            return;
        }

        try
        {
            var client = _http.CreateClient();
            client.DefaultRequestHeaders.Add("api-key", _brevo.ApiKey);

            // updateEnabled=true: Brevo updates an existing contact in-place instead of
            // returning duplicate_parameter. With this set, any 4xx response is a real
            // error (bad payload, invalid attribute) that deserves a log entry.
            var payload = new
            {
                email,
                attributes = new { FIRSTNAME = firstName ?? string.Empty },
                listIds = new[] { _brevo.ListId!.Value },
                updateEnabled = true
            };

            var response = await client.PostAsJsonAsync("https://api.brevo.com/v3/contacts", payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "Brevo returned {Status} for {Email}: {Body}",
                    (int)response.StatusCode, email, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add {Email} to Brevo list {ListId}", email, _brevo.ListId);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException pg && pg.SqlState == PostgresUniqueViolation;
}
