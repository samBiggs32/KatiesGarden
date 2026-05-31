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

namespace KatiesGarden.Api.Functions;

// GET /api/diagnostics — live readiness check for the deployed Function
// app's external dependencies. Anonymous so it can be called from uptime
// monitors and the Aspire resource health probe — rate-limited at the
// Cloudflare edge to protect integration quotas.
//
// Returns 200 with status="ready" when all configured deps are reachable;
// 503 with status="degraded" otherwise. "not_configured" is not a failure —
// optional services degrade gracefully.
//
// All checks are quota-safe: DB → SELECT 1, Brevo → GET /v3/account,
// Stripe → Balance.Get, Blob → GetProperties, SMTP → connect+auth+disconnect.
// SMTP is skipped by default (1–3s round-trip); pass ?checkSmtp=true to include.
public class DiagnosticsFunction(
    AppDbContext db,
    IServiceProvider services,
    IHttpClientFactory http,
    IOptions<BrevoOptions> brevoOptions,
    IOptions<StripeOptions> stripeOptions,
    IOptions<SmtpOptions> smtpOptions,
    ILogger<DiagnosticsFunction> logger)
{
    [Function("Diagnostics")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "diagnostics")] HttpRequestData req)
    {
        var ct = req.FunctionContext.CancellationToken;
        var includeSmtp = req.Url.Query.Contains("checkSmtp=true", StringComparison.OrdinalIgnoreCase);

        var checks = new Dictionary<string, string>
        {
            ["api"]          = "ok",
            ["database"]     = await CheckDatabase(ct),
            ["brevo_api"]    = await CheckBrevoApi(ct),
            ["stripe"]       = await CheckStripe(ct),
            ["blob_storage"] = await CheckBlobStorage(ct),
            ["smtp"]         = includeSmtp ? await CheckSmtp(ct) : "skipped"
        };

        var allHealthy = !checks.Values.Contains("fail");
        var response = req.CreateResponse(allHealthy ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable);
        response.Headers.Add("Content-Type", "application/json");
        response.Headers.Add("Cache-Control", "no-store");
        await response.WriteStringAsync(JsonSerializer.Serialize(new
        {
            status = allHealthy ? "ready" : "degraded",
            checks,
            timestamp = DateTimeOffset.UtcNow
        }));
        return response;
    }

    private async Task<string> CheckDatabase(CancellationToken ct)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync("SELECT 1", ct);
            return "ok";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Diagnostics: database check failed");
            return "fail";
        }
    }

    private async Task<string> CheckBrevoApi(CancellationToken ct)
    {
        var brevo = brevoOptions.Value;
        if (!brevo.IsConfigured) return "not_configured";

        try
        {
            using var client = http.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Add("api-key", brevo.ApiKey);
            using var resp = await client.GetAsync("https://api.brevo.com/v3/account", ct);
            return resp.IsSuccessStatusCode ? "ok" : "fail";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Diagnostics: Brevo API check failed");
            return "fail";
        }
    }

    private async Task<string> CheckStripe(CancellationToken ct)
    {
        var stripe = stripeOptions.Value;
        if (!stripe.IsConfigured) return "not_configured";

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));

            var balanceService = new BalanceService(new StripeClient(stripe.SecretKey));
            await balanceService.GetAsync(cancellationToken: timeout.Token);
            return "ok";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Diagnostics: Stripe check failed");
            return "fail";
        }
    }

    // BlobServiceClient is only registered when AZURE_STORAGE_CONNECTION_STRING is
    // present, so GetService<T>() returning null is the correct "not configured" signal.
    private async Task<string> CheckBlobStorage(CancellationToken ct)
    {
        var blob = services.GetService<BlobServiceClient>();
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
            logger.LogWarning(ex, "Diagnostics: Blob Storage check failed");
            return "fail";
        }
    }

    private async Task<string> CheckSmtp(CancellationToken ct)
    {
        var smtp = smtpOptions.Value;
        if (string.IsNullOrWhiteSpace(smtp.Host) ||
            string.IsNullOrWhiteSpace(smtp.Username) ||
            string.IsNullOrWhiteSpace(smtp.Password))
            return "not_configured";

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(8));

            using var client = new SmtpClient();
            await client.ConnectAsync(smtp.Host, smtp.Port, SecureSocketOptions.StartTlsWhenAvailable, timeout.Token);
            await client.AuthenticateAsync(smtp.Username, smtp.Password, timeout.Token);
            await client.DisconnectAsync(true, timeout.Token);
            return "ok";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Diagnostics: SMTP connectivity check failed");
            return "fail";
        }
    }
}
