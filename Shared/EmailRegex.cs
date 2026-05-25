namespace KatiesGarden.Web.Client.Models.Validators;

internal static class EmailRegex
{
    // Stricter than FluentValidation's default `AspNetCoreCompatible` mode,
    // which only checks for a single '@' not at the ends. This requires at
    // least one dot in the domain and forbids whitespace anywhere, catching
    // common typos like "user@tld" or "first name@example.com".
    public const string Pattern = @"^[^\s@]+@[^\s@]+\.[^\s@]+$";
}
