using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShopSphere.BuildingBlocks.Application.Messaging;
using ShopSphere.Catalog.Domain.Products;
using ShopSphere.Catalog.Infrastructure.Persistence;
using ShopSphere.Contracts.Events;

namespace ShopSphere.Catalog.Infrastructure;

/// <summary>Seeds a realistic demo catalog on first start and announces each product
/// through the outbox so Inventory starts tracking stock.</summary>
public static class CatalogSeeder
{
    public static async Task SeedAsync(IServiceProvider root)
    {
        using var scope = root.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        if (await db.Products.AnyAsync()) return;

        var events = scope.ServiceProvider.GetRequiredService<IIntegrationEventCollector>();
        var cats = new[]
        {
            Category.Create("Electronics", "electronics"), Category.Create("Fashion", "fashion"),
            Category.Create("Home & Kitchen", "home"), Category.Create("Sports", "sports")
        };
        db.Categories.AddRange(cats);

        (string, string, string, decimal, int, double, int)[] rows =
        {
            ("Wireless Noise-Cancelling Headphones", "EL-1001", "Over-ear Bluetooth headphones with 40h battery.", 3499m, 0, 4.7, 2310),
            ("Mechanical Keyboard 75%", "EL-1002", "Hot-swappable switches, RGB, USB-C.", 2199m, 0, 4.6, 980),
            ("4K Action Camera", "EL-1003", "Waterproof, stabilised, 5K video.", 5999m, 0, 4.4, 640),
            ("Smart Watch Series X", "EL-1004", "AMOLED display, GPS, heart-rate tracking.", 4299m, 0, 4.5, 1520),
            ("Slim Fit Denim Jacket", "FA-2001", "Stretch denim, washed finish.", 1299m, 1, 4.3, 410),
            ("Running Sneakers Air", "FA-2002", "Lightweight breathable mesh.", 1899m, 1, 4.6, 2210),
            ("Leather Crossbody Bag", "FA-2003", "Genuine leather with adjustable strap.", 1599m, 1, 4.5, 530),
            ("Espresso Machine Pro", "HM-3001", "15-bar pump, milk frother.", 7999m, 2, 4.7, 890),
            ("Air Fryer 6L", "HM-3002", "Digital controls, 8 presets.", 2499m, 2, 4.6, 3120),
            ("Memory Foam Pillow", "HM-3003", "Ergonomic, cooling cover.", 449m, 2, 4.4, 1750),
            ("Demo: Payment Decline Test Item", "DM-9999", "Priced to end in .99 so the simulated bank declines it. Use it to demo the cancellation and stock-release path.", 99.99m, 2, 4.0, 12),
            ("Yoga Mat Non-Slip", "SP-4001", "6mm thick, carry strap included.", 399m, 3, 4.5, 960),
            ("Adjustable Dumbbell Set", "SP-4002", "2-24 kg quick-change.", 6499m, 3, 4.8, 720),
        };

        foreach (var (name, sku, desc, price, cat, rating, reviews) in rows)
        {
            var p = Product.Create(name, sku, desc, price, "EGP",
                $"/products/{sku}.svg", cats[cat].Id, rating, reviews);
            db.Products.Add(p);
            events.Collect(new ProductCreatedIntegrationEvent(p.Id, p.Name, p.Sku, p.Price, p.Currency, p.CategoryId));
        }
        await db.SaveChangesAsync();
    }
}
