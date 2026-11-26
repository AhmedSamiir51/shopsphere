using Microsoft.EntityFrameworkCore;
using ShopSphere.BuildingBlocks.Application.Abstractions;
using ShopSphere.BuildingBlocks.EventBus.Outbox;
using ShopSphere.Ordering.Domain.Orders;
using ShopSphere.Ordering.Domain.ValueObjects;

namespace ShopSphere.Ordering.Infrastructure.Persistence;

public sealed class OrderingDbContext : DbContext, IOutboxDbContext, IUnitOfWork
{
    public OrderingDbContext(DbContextOptions<OrderingDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<ProductSnapshot> ProductSnapshots => Set<ProductSnapshot>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Order>(o =>
        {
            o.ToTable("orders");
            o.HasKey(x => x.Id);
            o.Property(x => x.Currency).HasMaxLength(3);
            o.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            o.Property(x => x.CancellationReason).HasMaxLength(500);
            o.Ignore(x => x.Total);
            o.Ignore(x => x.DomainEvents);
            o.HasMany(x => x.Items).WithOne().HasForeignKey("OrderId").OnDelete(DeleteBehavior.Cascade);
            o.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
            o.HasIndex(x => x.UserId);
        });

        b.Entity<OrderItem>(i =>
        {
            i.ToTable("order_items");
            i.HasKey(x => x.Id);
            i.Ignore(x => x.DomainEvents);
            i.Ignore(x => x.LineTotal);
            i.Property(x => x.ProductName).HasMaxLength(200);
            i.OwnsOne(x => x.UnitPrice, m =>
            {
                m.Property(p => p.Amount).HasColumnName("unit_price").HasPrecision(18, 2);
                m.Property(p => p.Currency).HasColumnName("unit_currency").HasMaxLength(3);
            });
        });

        b.Entity<ProductSnapshot>(p =>
        {
            p.ToTable("product_snapshots");
            p.HasKey(x => x.ProductId);
            p.Property(x => x.Name).HasMaxLength(200);
            p.Property(x => x.Currency).HasMaxLength(3);
            p.Property(x => x.Price).HasPrecision(18, 2);
        });

        b.Entity<OutboxMessage>(m =>
        {
            m.ToTable("outbox_messages");
            m.HasKey(x => x.Id);
            m.HasIndex(x => x.ProcessedOnUtc);
        });
    }
}
