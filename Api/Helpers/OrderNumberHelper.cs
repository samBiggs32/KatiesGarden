namespace KatiesGarden.Api.Helpers;

public static class OrderNumberHelper
{
    public static string Generate()
    {
        var suffix = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
        return $"KG-{DateTime.UtcNow:yyyyMMdd}-{suffix}";
    }
}
