using Microsoft.EntityFrameworkCore;
using ShopSphere.BuildingBlocks.Application.Abstractions;
using ShopSphere.BuildingBlocks.EventBus.Outbox;
using ShopSphere.Notification.Domain.Notifications;

namespace ShopSphere.Notification.Infrastructure.Persistence;

public sealed class NotificationDbContext : DbContext, IOutboxDbContext, IUnitOfWork
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<Domain.Notifications.Notification> Notifications => Set<Domain.Notifications.Notification>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Domain.Notifications.Notification>(n =>
        {
            n.ToTable("notifications");
            n.HasKey(x => x.Id);
            n.HasIndex(x => x.UserId);
            n.Ignore(x => x.DomainEvents);
            n.Property(x => x.Channel).HasConversion<string>().HasMaxLength(20);
            n.Property(x => x.Subject).HasMaxLength(200);
        });
        b.Entity<OutboxMessage>(m => { m.ToTable("outbox_messages"); m.HasKey(x => x.Id); });
    }
}
