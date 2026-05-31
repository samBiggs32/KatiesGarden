using System.Text.RegularExpressions;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace KatiesGarden.Api.Telemetry;

/// <summary>
/// Strips personal data from telemetry before it leaves the process. Request URLs can
/// carry emails or search terms in the query string (e.g. ?email=...&amp;q=...); these are
/// redacted so they are never stored in Application Insights. This is the standard
/// App Insights extensibility point for this purpose.
/// </summary>
public partial class PiiScrubbingInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        if (telemetry is RequestTelemetry request && request.Url is not null)
            request.Url = new Uri(Scrub(request.Url.ToString()));
    }

    private static string Scrub(string url)
    {
        url = EmailPattern().Replace(url, "[email]");
        url = SensitiveQueryPattern().Replace(url, "$1=[redacted]");
        return url;
    }

    [GeneratedRegex(@"[\w.+-]+@[\w-]+\.[\w.-]+", RegexOptions.IgnoreCase)]
    private static partial Regex EmailPattern();

    // Redacts the value of known PII-bearing query keys (email, q) regardless of value.
    [GeneratedRegex(@"\b(email|q)=[^&]*", RegexOptions.IgnoreCase)]
    private static partial Regex SensitiveQueryPattern();
}
