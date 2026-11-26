using Microsoft.EntityFrameworkCore;
using ShopSphere.BuildingBlocks.Application.Abstractions;
using ShopSphere.BuildingBlocks.EventBus.Outbox;
using ShopSphere.Inventory.Domain.Stock;

namespace ShopSphere.Inventory.Infrastructure.Persistence;

public sealed class InventoryDbContext : DbContext, IOutboxDbContext, IUnitOfWork
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<StockItem>(s =>
        {
            s.ToTable("stock_items");
            s.HasKey(x => x.Id);
            s.HasIndex(x => x.ProductId).IsUnique();
            s.Ignore(x => x.DomainEvents);
            s.Property<uint>("xmin").IsRowVersion(); // optimistic concurrency on Postgres
        });
        b.Entity<Reservation>(r =>
        {
            r.ToTable("reservations");
            r.HasKey(x => x.Id);
            r.HasIndex(x => x.OrderId).IsUnique();
            r.Ignore(x => x.DomainEvents);
            r.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            r.OwnsMany(x => x.Lines, l => { l.ToTable("reservation_lines"); l.WithOwner().HasForeignKey("ReservationId"); l.Property<int>("Id"); l.HasKey("Id"); });
        });
        b.Entity<OutboxMessage>(m => { m.ToTable("outbox_messages"); m.HasKey(x => x.Id); m.HasIndex(x => x.ProcessedOnUtc); });
    }
}
