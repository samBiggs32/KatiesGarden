using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KatiesGarden.Api.Data;

// Design-time factory so `dotnet ef` can instantiate AppDbContext without
// booting the full Azure Functions host. Only used during migration generation;
// the real connection string is injected at runtime via DATABASE_URL.
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=katiesgarden_design;Username=postgres;Password=postgres")
            .Options;
        return new AppDbContext(opts);
    }
}
