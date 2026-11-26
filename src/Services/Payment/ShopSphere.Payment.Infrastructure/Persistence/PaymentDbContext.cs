using Microsoft.EntityFrameworkCore;
using ShopSphere.BuildingBlocks.Application.Abstractions;
using ShopSphere.BuildingBlocks.EventBus.Outbox;
using ShopSphere.Payment.Domain.Payments;

namespace ShopSphere.Payment.Infrastructure.Persistence;

public sealed class PaymentDbContext : DbContext, IOutboxDbContext, IUnitOfWork
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<PaymentRecord> Payments => Set<PaymentRecord>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<PaymentRecord>(p =>
        {
            p.ToTable("payments");
            p.HasKey(x => x.Id);
            p.HasIndex(x => x.OrderId).IsUnique();
            p.Ignore(x => x.DomainEvents);
            p.Property(x => x.Amount).HasPrecision(18, 2);
            p.Property(x => x.Currency).HasMaxLength(3);
            p.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        });
        b.Entity<OutboxMessage>(m => { m.ToTable("outbox_messages"); m.HasKey(x => x.Id); m.HasIndex(x => x.ProcessedOnUtc); });
    }
}
