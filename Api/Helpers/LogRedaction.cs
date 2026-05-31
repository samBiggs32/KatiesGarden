using System.Security.Cryptography;
using System.Text;

namespace KatiesGarden.Api.Helpers;

/// <summary>
/// Helpers for keeping personal data out of logs and telemetry. Emails and other
/// identifiers are hashed before logging so an entry can still be correlated across
/// log lines (same input → same hash) without exposing the value itself.
/// </summary>
public static class LogRedaction
{
    /// <summary>
    /// Returns a short, stable token for a value: the first 12 hex chars of its
    /// SHA-256. Safe to log — not reversible, but consistent for correlation.
    /// </summary>
    public static string Hash(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "(none)";

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim().ToLowerInvariant()));
        return Convert.ToHexString(bytes)[..12].ToLowerInvariant();
    }
}
