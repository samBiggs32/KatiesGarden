using KatiesGarden.Models.Auth;
using Microsoft.Azure.Functions.Worker.Http;
using System.Security.Cryptography;
using System.Text;

namespace KatiesGarden.Api.Auth;

public static class SwaAuth
{
    // The local-dev admin bypass exists because, when running via Aspire on localhost,
    // the SWA /.auth/* layer doesn't inject an x-ms-client-principal header. The bypass
    // makes the admin API usable locally without the SWA CLI.
    //
    // Defence-in-depth so a misconfigured ASPNETCORE_ENVIRONMENT can never open the admin
    // API in a deployed environment:
    //   1. The process must be in Development, AND
    //   2. It must NOT be running in Azure (Azure App Service/Functions always set
    //      WEBSITE_INSTANCE_ID; localhost never does), AND
    //   3. If DEV_BYPASS_SECRET is configured, the caller must present it in X-Dev-Bypass.
    //      (Lets a shared, non-Azure dev box require an explicit token; unset on a normal
    //      localhost machine, so the default dev workflow is unchanged.)
    private static readonly bool _isDev =
        string.Equals(
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            "Development",
            StringComparison.OrdinalIgnoreCase);

    private static readonly bool _runningInAzure =
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"));

    private static readonly string? _devBypassSecret =
        Environment.GetEnvironmentVariable("DEV_BYPASS_SECRET");

    private static bool IsDevBypass(HttpRequestData req)
    {
        if (!_isDev || _runningInAzure)
            return false;

        if (string.IsNullOrEmpty(_devBypassSecret))
            return true; // plain localhost dev — no secret configured

        return req.Headers.TryGetValues("X-Dev-Bypass", out var values)
            && CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(values.First()),
                Encoding.UTF8.GetBytes(_devBypassSecret));
    }

    public static ClientPrincipal? GetPrincipal(HttpRequestData req)
    {
        if (!req.Headers.TryGetValues("x-ms-client-principal", out var values))
            return null;

        return ClientPrincipal.Decode(values.First());
    }

    public static bool IsAdmin(HttpRequestData req) =>
        IsDevBypass(req) || (GetPrincipal(req)?.IsAdmin ?? false);

    // Returns the principal for any authenticated user (any OAuth provider).
    // In local development, returns the dev bypass principal.
    public static ClientPrincipal? GetAuthenticatedUser(HttpRequestData req)
    {
        if (IsDevBypass(req))
            return new ClientPrincipal("dev", "local-dev", "local-dev",
                ["anonymous", "authenticated", "admin"]);

        var principal = GetPrincipal(req);
        if (principal?.UserRoles?.Contains("authenticated", StringComparer.OrdinalIgnoreCase) == true)
            return principal;
        return null;
    }
}
