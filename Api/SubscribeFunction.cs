using FluentValidation;
using KatiesGarden.Api.Data;
using KatiesGarden.Web.Client.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;

namespace KatiesGarden.Api;

public class SubscribeFunction
{
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
        SubscribeRequest? request;
        try { request = await req.ReadFromJsonAsync<SubscribeRequest>(); }
        catch
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Invalid request body.");
            return bad;
        }

        if (request is null)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Request body is required.");
            return bad;
        }

        var validation = _validator.Validate(request);
        if (!validation.IsValid)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync(validation.Errors.First().ErrorMessage);
            return bad;
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var firstName = request.FirstName?.Trim();

        await SaveToDatabase(email, firstName);
        await AddToBrevo(email, firstName);

        _logger.LogInformation("Newsletter subscription: {Email}", email);
        return req.CreateResponse(HttpStatusCode.OK);
    }

    private async Task SaveToDatabase(string email, string? firstName)
    {
        var db = _services.GetService<AppDbContext>();
        if (db is null) return;

        try
        {
            var exists = await db.Subscribers.AnyAsync(s => s.Email == email);
            if (!exists)
            {
                db.Subscribers.Add(new Subscriber { Email = email, FirstName = firstName });
                await db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save subscriber {Email} to database", email);
        }
    }

    private async Task AddToBrevo(string email, string? firstName)
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

            var response = await client.PostAsJsonAsync("https://api.brevo.com/v3/contacts", payload);

            if (!response.IsSuccessStatusCode && (int)response.StatusCode != 400)
                response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add {Email} to Brevo list {ListId}", email, _brevoListId);
        }
    }
}
