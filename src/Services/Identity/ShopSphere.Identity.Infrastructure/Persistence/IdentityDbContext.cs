using Microsoft.EntityFrameworkCore;
using ShopSphere.BuildingBlocks.Application.Abstractions;
using ShopSphere.BuildingBlocks.EventBus.Outbox;
using ShopSphere.Identity.Domain.Users;

namespace ShopSphere.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext : DbContext, IOutboxDbContext, IUnitOfWork
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(u =>
        {
            u.ToTable("users");
            u.HasKey(x => x.Id);
            u.HasIndex(x => x.Email).IsUnique();
            u.Ignore(x => x.DomainEvents);
            u.Property(x => x.Email).HasMaxLength(256);
            u.Property(x => x.UserName).HasMaxLength(60);
            u.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
        });
        b.Entity<OutboxMessage>(m => { m.ToTable("outbox_messages"); m.HasKey(x => x.Id); m.HasIndex(x => x.ProcessedOnUtc); });
    }
}
