using KatiesGarden.Api.Configuration;
using KatiesGarden.Api.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;

namespace KatiesGarden.Api;

// GET /api/diagnostics — live readiness check for the deployed Function
// app's external dependencies. Public + anonymous so it can be called
// from uptime monitors and from a terminal, but rate-limited at the
// Cloudflare edge to keep our Brevo API quota safe from abuse.
//
// Returns 200 with status="ready" when all configured dependencies are
// reachable; 503 with status="degraded" otherwise. Individual checks
// report "ok", "fail", or "not_configured" — the last is not a failure
// (the subscribe endpoint degrades gracefully without Neon or Brevo).
//
// SMTP authentication is deliberately NOT tested here: a full STARTTLS
// + AUTH LOGIN round-trip takes 1–3 seconds, which is too slow for an
// endpoint that uptime monitors will hit every minute. The daily
// verify-secrets GitHub Action covers SMTP.
public class DiagnosticsFunction
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _services;
    private readonly IHttpClientFactory _http;
    private readonly BrevoOptions _brevo;

    public DiagnosticsFunction(
        ILoggerFactory loggerFactory,
        IServiceProvider services,
        IHttpClientFactory http,
        IOptions<BrevoOptions> brevoOptions)
    {
        _logger = loggerFactory.CreateLogger<DiagnosticsFunction>();
        _services = services;
        _http = http;
        _brevo = brevoOptions.Value;
    }

    [Function("Diagnostics")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "diagnostics")] HttpRequestData req)
    {
        var ct = req.FunctionContext.CancellationToken;

        var checks = new Dictionary<string, string>
        {
            ["api"] = "ok",
            ["database"] = await CheckDatabase(ct),
            ["brevo_api"] = await CheckBrevoApi(ct),
            ["smtp"] = "skipped"
        };

        var allHealthy = !checks.Values.Contains("fail");
        var status = allHealthy ? "ready" : "degraded";

        var response = req.CreateResponse(allHealthy ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable);
        response.Headers.Add("Content-Type", "application/json");
        response.Headers.Add("Cache-Control", "no-store");
        await response.WriteStringAsync(JsonSerializer.Serialize(new
        {
            status,
            checks,
            timestamp = DateTimeOffset.UtcNow
        }));
        return response;
    }

    private async Task<string> CheckDatabase(CancellationToken ct)
    {
        var db = _services.GetService<AppDbContext>();
        if (db is null) return "not_configured";

        try
        {
            await db.Database.ExecuteSqlRawAsync("SELECT 1", ct);
            return "ok";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Diagnostics: database check failed");
            return "fail";
        }
    }

    private async Task<string> CheckBrevoApi(CancellationToken ct)
    {
        if (!_brevo.IsConfigured) return "not_configured";

        try
        {
            using var client = _http.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Add("api-key", _brevo.ApiKey);
            using var resp = await client.GetAsync("https://api.brevo.com/v3/account", ct);
            return resp.IsSuccessStatusCode ? "ok" : "fail";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Diagnostics: Brevo API check failed");
            return "fail";
        }
    }
}
