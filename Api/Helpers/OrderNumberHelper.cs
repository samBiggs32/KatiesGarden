using KatiesGarden.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace KatiesGarden.Api.Helpers;

public static class OrderNumberHelper
{
    public static async Task<string> GenerateAsync(AppDbContext db, CancellationToken ct = default)
    {
        var year = DateTime.UtcNow.Year;
        var count = await db.Orders
            .Where(o => o.CreatedAt.Year == year)
            .CountAsync(ct);
        return $"KG-{year}-{count + 1:D3}";
    }
}
