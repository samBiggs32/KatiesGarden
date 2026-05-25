using Microsoft.EntityFrameworkCore;

namespace KatiesGarden.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Subscriber> Subscribers => Set<Subscriber>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Subscriber>(e =>
        {
            e.ToTable("subscribers");
            e.HasIndex(s => s.Email).IsUnique();
            e.Property(s => s.Email).HasMaxLength(254).IsRequired();
            e.Property(s => s.FirstName).HasMaxLength(100);
        });
    }
}
