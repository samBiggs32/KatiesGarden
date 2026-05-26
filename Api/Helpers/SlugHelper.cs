using System.Text.RegularExpressions;

namespace KatiesGarden.Api.Helpers;

public static class SlugHelper
{
    private static readonly Regex NonAlphanumeric = new(@"[^a-z0-9]+", RegexOptions.Compiled);

    public static string Generate(string title)
        => NonAlphanumeric.Replace(title.ToLowerInvariant().Trim(), "-").Trim('-');
}
