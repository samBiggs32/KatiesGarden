using KatiesGarden.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KatiesGarden.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Subscriber> Subscribers => Set<Subscriber>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<DeliverySettings> DeliverySettings => Set<DeliverySettings>();
    public DbSet<StorePushSubscription> PushSubscriptions => Set<StorePushSubscription>();
    public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<StripeProcessedEvent> StripeProcessedEvents => Set<StripeProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Subscriber>(e =>
        {
            e.ToTable("subscribers");
            e.HasIndex(s => s.Email).IsUnique();
            e.Property(s => s.Email).HasMaxLength(254).IsRequired();
            e.Property(s => s.FirstName).HasMaxLength(100);
        });

        modelBuilder.Entity<Collection>(e =>
        {
            e.ToTable("collections");
            e.HasIndex(c => c.Slug).IsUnique();
            e.Property(c => c.Title).HasMaxLength(200).IsRequired();
            e.Property(c => c.Slug).HasMaxLength(200).IsRequired();
            e.Property(c => c.Description).HasMaxLength(2000);
            e.HasMany(c => c.Products)
             .WithOne(p => p.Collection)
             .HasForeignKey(p => p.CollectionId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("products");
            e.HasIndex(p => p.Slug).IsUnique();
            e.Property(p => p.Name).HasMaxLength(200).IsRequired();
            e.Property(p => p.Slug).HasMaxLength(200).IsRequired();
            e.Property(p => p.Description).HasMaxLength(2000);
            e.Property(p => p.HowToBuyNote).HasMaxLength(500);
            e.Property(p => p.Price).HasPrecision(10, 2);
            e.Property(p => p.ImageUrls).HasColumnType("text[]");
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.ToTable("orders");
            e.HasIndex(o => o.OrderNumber).IsUnique();
            e.HasIndex(o => o.CustomerId);
            e.Property(o => o.OrderNumber).HasMaxLength(20).IsRequired();
            e.Property(o => o.CustomerFirstName).HasMaxLength(100).IsRequired();
            e.Property(o => o.CustomerLastName).HasMaxLength(100).IsRequired();
            e.Property(o => o.CustomerEmail).HasMaxLength(254).IsRequired();
            e.Property(o => o.CustomerPhone).HasMaxLength(30).IsRequired();
            e.Property(o => o.DeliveryAddress).HasMaxLength(500);
            e.Property(o => o.DeliveryPostcode).HasMaxLength(10);
            e.Property(o => o.CustomerNotes).HasMaxLength(1000);
            e.Property(o => o.AdminNotes).HasMaxLength(2000);
            e.Property(o => o.CustomerId).HasMaxLength(100);
            e.Property(o => o.CustomerIdentityProvider).HasMaxLength(50);
            e.Property(o => o.OrchestrationInstanceId).HasMaxLength(200);
            e.Property(o => o.Subtotal).HasPrecision(10, 2);
            e.Property(o => o.DeliveryFee).HasPrecision(10, 2);
            e.Property(o => o.Total).HasPrecision(10, 2);
            e.HasMany(o => o.Lines)
             .WithOne(l => l.Order)
             .HasForeignKey(l => l.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(o => o.StatusHistory)
             .WithOne(h => h.Order)
             .HasForeignKey(h => h.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderLine>(e =>
        {
            e.ToTable("order_lines");
            e.Property(l => l.ProductName).HasMaxLength(200).IsRequired();
            e.Property(l => l.UnitPrice).HasPrecision(10, 2);
            e.Property(l => l.LineTotal).HasPrecision(10, 2);
        });

        modelBuilder.Entity<DeliverySettings>(e =>
        {
            e.ToTable("delivery_settings");
            e.Property(d => d.LocalDeliveryFee).HasPrecision(10, 2);
            e.Property(d => d.FreeDeliveryThreshold).HasPrecision(10, 2);
            e.Property(d => d.DeliveryAreaDescription).HasMaxLength(500);
            e.Property(d => d.CollectionAddress).HasMaxLength(300);
            e.Property(d => d.CollectionInstructions).HasMaxLength(1000);
        });

        modelBuilder.Entity<StorePushSubscription>(e =>
        {
            e.ToTable("push_subscriptions");
            e.HasIndex(p => p.Endpoint);
            e.Property(p => p.Endpoint).HasMaxLength(500).IsRequired();
            e.Property(p => p.P256dh).HasMaxLength(200).IsRequired();
            e.Property(p => p.Auth).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<OrderStatusHistory>(e =>
        {
            e.ToTable("order_status_history");
            e.HasIndex(h => new { h.OrderId, h.ChangedAt });
            e.Property(h => h.Note).HasMaxLength(500);
            e.Property(h => h.ChangedBy).HasMaxLength(254);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_logs");
            e.HasIndex(a => a.Timestamp);
            e.HasIndex(a => new { a.EntityType, a.EntityId });
            e.Property(a => a.Action).HasMaxLength(100).IsRequired();
            e.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
            e.Property(a => a.EntityId).HasMaxLength(100).IsRequired();
            e.Property(a => a.ActorEmail).HasMaxLength(254);
            e.Property(a => a.ActorName).HasMaxLength(200);
            e.Property(a => a.Details).HasColumnType("text");
        });

        modelBuilder.Entity<StripeProcessedEvent>(e =>
        {
            e.ToTable("stripe_processed_events");
            e.HasKey(s => s.EventId);
            e.Property(s => s.EventId).HasMaxLength(100).IsRequired();
        });
    }
}
