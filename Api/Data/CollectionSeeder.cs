using KatiesGarden.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KatiesGarden.Api.Data;

/// <summary>
/// Seeds a starter set of seasonal collections so the shop has structure out of the box.
/// Idempotent: only inserts seasons whose slug is not already present, and never touches
/// collections Katie has created or edited. Seeds are inactive so they don't appear on the
/// public storefront while empty — Katie assigns products, then activates them.
/// </summary>
public static class CollectionSeeder
{
    private static readonly (string Title, string Slug, string Description)[] Seasons =
    [
        ("Spring", "spring", "Fresh growth for the new season — bulbs, bedding, and early colour to wake up the garden."),
        ("Summer", "summer", "Long days and full borders — vibrant blooms, planters, and pieces for outdoor living."),
        ("Autumn", "autumn", "Warm tones and the harvest season — hardy planting and handcrafted woodwork for cooler days."),
        ("Winter", "winter", "Structure and shelter — evergreens, bug houses, and gifts to see the garden through winter."),
    ];

    public static async Task SeedAsync(AppDbContext db, ILogger log, CancellationToken ct)
    {
        var existingSlugs = await db.Collections.Select(c => c.Slug).ToListAsync(ct);

        var toAdd = new List<Collection>();
        for (var i = 0; i < Seasons.Length; i++)
        {
            var (title, slug, description) = Seasons[i];
            if (existingSlugs.Contains(slug)) continue;

            toAdd.Add(new Collection
            {
                Title = title,
                Slug = slug,
                Description = description,
                IsActive = false,   // hidden from the public shop until Katie adds products and activates
                DisplayOrder = i + 1,
                StartDate = DateTime.UtcNow
            });
        }

        if (toAdd.Count == 0) return;

        db.Collections.AddRange(toAdd);
        await db.SaveChangesAsync(ct);
        log.LogInformation("Seeded {Count} starter collection(s): {Titles}",
            toAdd.Count, string.Join(", ", toAdd.Select(c => c.Title)));
    }
}
