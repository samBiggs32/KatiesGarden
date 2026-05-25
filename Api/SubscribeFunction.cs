using FluentValidation;
using KatiesGarden.Api.Data;
using KatiesGarden.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    private readonly string? _brevoApiKey;
    private readonly int? _brevoListId;

    public SubscribeFunction(
        ILoggerFactory loggerFactory,
        IServiceProvider services,
        IHttpClientFactory http,
        IValidator<SubscribeRequest> validator,
        IConfiguration config)
    {
        _logger = loggerFactory.CreateLogger<SubscribeFunction>();
        _services = services;
        _http = http;
        _validator = validator;
        _brevoApiKey = config["BREVO_API_KEY"];
        _brevoListId = int.TryParse(config["BREVO_LIST_ID"], out var id) ? id : null;
    }

    [Function("Subscribe")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "subscribe")] HttpRequestData req)
    {
        var ct = req.FunctionContext.CancellationToken;

        SubscribeRequest? request;
        try { request = await req.ReadFromJsonAsync<SubscribeRequest>(); }
        catch
        {
            return await Responses.BadRequest(req, "Invalid request body.");
        }

        if (request is null)
            return await Responses.BadRequest(req, "Request body is required.");

        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return await Responses.BadRequest(req, validation.Errors.First().ErrorMessage);

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
        if (db is null) return;

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
        if (string.IsNullOrWhiteSpace(_brevoApiKey) || _brevoListId is null) return;

        try
        {
            var client = _http.CreateClient();
            client.DefaultRequestHeaders.Add("api-key", _brevoApiKey);

            var payload = new
            {
                email,
                attributes = new { FIRSTNAME = firstName ?? string.Empty },
                listIds = new[] { _brevoListId.Value },
                updateEnabled = true
            };

            var response = await client.PostAsJsonAsync("https://api.brevo.com/v3/contacts", payload, ct);

            // Brevo returns 400 when the contact already exists in the list — that's
            // a benign success for us (the email IS on the list). Anything else, raise.
            if (!response.IsSuccessStatusCode && (int)response.StatusCode != 400)
                response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add {Email} to Brevo list {ListId}", email, _brevoListId);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException pg && pg.SqlState == PostgresUniqueViolation;
}
