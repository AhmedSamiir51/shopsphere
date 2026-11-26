using Microsoft.EntityFrameworkCore;
using ShopSphere.BuildingBlocks.Application.Abstractions;
using ShopSphere.BuildingBlocks.EventBus.Outbox;
using ShopSphere.Catalog.Domain.Products;

namespace ShopSphere.Catalog.Infrastructure.Persistence;

public sealed class CatalogDbContext : DbContext, IOutboxDbContext, IUnitOfWork
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Product>(p =>
        {
            p.ToTable("products");
            p.HasKey(x => x.Id);
            p.HasIndex(x => x.Sku).IsUnique();
            p.HasIndex(x => x.CategoryId);
            p.Ignore(x => x.DomainEvents);
            p.Property(x => x.Name).HasMaxLength(200);
            p.Property(x => x.Sku).HasMaxLength(50);
            p.Property(x => x.Price).HasPrecision(18, 2);
            p.Property(x => x.Currency).HasMaxLength(3);
        });
        b.Entity<Category>(c =>
        {
            c.ToTable("categories");
            c.HasKey(x => x.Id);
            c.HasIndex(x => x.Slug).IsUnique();
            c.Ignore(x => x.DomainEvents);
        });
        b.Entity<OutboxMessage>(m => { m.ToTable("outbox_messages"); m.HasKey(x => x.Id); m.HasIndex(x => x.ProcessedOnUtc); });
    }
}
