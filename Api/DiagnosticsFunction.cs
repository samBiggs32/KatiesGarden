using Azure.Storage.Blobs;
using KatiesGarden.Api.Configuration;
using KatiesGarden.Api.Data;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using System.Net;
using System.Text.Json;

namespace KatiesGarden.Api;

// GET /api/diagnostics — live readiness check for the deployed Function
// app's external dependencies. Public + anonymous so it can be called
// from uptime monitors, from a terminal, and as the Aspire resource health
// probe — but rate-limited at the Cloudflare edge to keep our integration
// quotas safe from abuse.
//
// Returns 200 with status="ready" when all configured dependencies are
// reachable; 503 with status="degraded" otherwise. Individual checks
// report "ok", "fail", or "not_configured" — the last is NOT a failure
// (endpoints degrade gracefully without Neon/Brevo/Stripe/Blob, and the
// Aspire AppHost injects placeholders locally so the stack boots without
// any real accounts).
//
// QUOTA SAFETY: every check is read-only and free —
//   * database  → SELECT 1
//   * brevo_api → GET /v3/account (account info, not an email send)
//   * stripe    → Balance.Get   (read-only, no charge created)
//   * blob      → service GetProperties (no container/blob writes)
//   * smtp      → connect + AUTH + disconnect, NO message sent
// Nothing here consumes send quota or bills against any account.
//
// SMTP is skipped by default because a full STARTTLS + AUTH round-trip
// takes 1–3s — too slow for an uptime monitor hitting this every minute.
// Pass ?checkSmtp=true to include it (used by the deeper health probes).
public class DiagnosticsFunction
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _services;
    private readonly IHttpClientFactory _http;
    private readonly BrevoOptions _brevo;
    private readonly StripeOptions _stripe;
    private readonly SmtpOptions _smtp;

    public DiagnosticsFunction(
        ILoggerFactory loggerFactory,
        IServiceProvider services,
        IHttpClientFactory http,
        IOptions<BrevoOptions> brevoOptions,
        IOptions<StripeOptions> stripeOptions,
        IOptions<SmtpOptions> smtpOptions)
    {
        _logger = loggerFactory.CreateLogger<DiagnosticsFunction>();
        _services = services;
        _http = http;
        _brevo = brevoOptions.Value;
        _stripe = stripeOptions.Value;
        _smtp = smtpOptions.Value;
    }

    [Function("Diagnostics")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "diagnostics")] HttpRequestData req)
    {
        var ct = req.FunctionContext.CancellationToken;

        var includeSmtp = req.Url.Query.Contains("checkSmtp=true", StringComparison.OrdinalIgnoreCase);

        var checks = new Dictionary<string, string>
        {
            ["api"] = "ok",
            ["database"] = await CheckDatabase(ct),
            ["brevo_api"] = await CheckBrevoApi(ct),
            ["stripe"] = await CheckStripe(ct),
            ["blob_storage"] = await CheckBlobStorage(ct),
            ["smtp"] = includeSmtp ? await CheckSmtp(ct) : "skipped"
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

    // Read-only Balance.Get — proves the secret key is live and accepted by
    // Stripe without creating a PaymentIntent, charge, or Checkout Session.
    private async Task<string> CheckStripe(CancellationToken ct)
    {
        if (!_stripe.IsConfigured) return "not_configured";

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));

            var balanceService = new BalanceService(new StripeClient(_stripe.SecretKey));
            await balanceService.GetAsync(cancellationToken: timeout.Token);
            return "ok";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Diagnostics: Stripe check failed");
            return "fail";
        }
    }

    // Read-only account GetProperties — proves the storage connection string
    // is valid and the account is reachable without creating containers/blobs.
    private async Task<string> CheckBlobStorage(CancellationToken ct)
    {
        var blob = _services.GetService<BlobServiceClient>();
        if (blob is null) return "not_configured";

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));

            await blob.GetPropertiesAsync(timeout.Token);
            return "ok";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Diagnostics: Blob Storage check failed");
            return "fail";
        }
    }

    // Connect + AUTH + disconnect only — verifies the SMTP host and credentials
    // are live WITHOUT sending a message, so it never touches send quota.
    private async Task<string> CheckSmtp(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_smtp.Host) ||
            string.IsNullOrWhiteSpace(_smtp.Username) ||
            string.IsNullOrWhiteSpace(_smtp.Password))
        {
            return "not_configured";
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(8));

            using var client = new SmtpClient();
            await client.ConnectAsync(_smtp.Host, _smtp.Port, SecureSocketOptions.StartTlsWhenAvailable, timeout.Token);
            await client.AuthenticateAsync(_smtp.Username, _smtp.Password, timeout.Token);
            await client.DisconnectAsync(true, timeout.Token);
            return "ok";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Diagnostics: SMTP connectivity check failed");
            return "fail";
        }
    }
}
