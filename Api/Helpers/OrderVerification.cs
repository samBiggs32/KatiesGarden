using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace KatiesGarden.Api.Helpers;

/// <summary>
/// Second-factor check for guest order lookup. The order-number space is small
/// (KG-YYYYMMDD-XXXX = 65,536/day), so order number + email alone is brute-forceable.
/// Requiring the order total means an attacker must also know the exact amount paid.
/// </summary>
public static class OrderVerification
{
    /// <summary>
    /// True if <paramref name="input"/> represents the same monetary amount as
    /// <paramref name="total"/>. Both sides are reduced to digits only, so "£24.99",
    /// "24.99" and "2499" all match. Compared in fixed time to avoid leaking the value.
    /// </summary>
    public static bool TotalMatches(decimal total, string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var expected = Digits(total.ToString("F2", CultureInfo.InvariantCulture));
        var actual = Digits(input);

        return actual.Length > 0
            && CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(actual),
                Encoding.UTF8.GetBytes(expected));
    }

    private static string Digits(string value) =>
        new(value.Where(char.IsDigit).ToArray());
}
