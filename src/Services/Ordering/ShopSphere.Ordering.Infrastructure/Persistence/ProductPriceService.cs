using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Ordering.Application.Abstractions;

namespace ShopSphere.Ordering.Infrastructure.Persistence;

public sealed class ProductPriceService(OrderingDbContext db, HttpClient catalog) : IProductPriceService
{
    private sealed record CatalogProduct(Guid Id, string Name, decimal Price, string Currency);

    public async Task<ProductPrice?> GetAsync(Guid productId, CancellationToken ct)
    {
        var row = await db.ProductSnapshots.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == productId, ct);
        if (row is not null) return Map(row);

        // Read-through: ask Catalog once, remember the answer.
        try
        {
            var p = await catalog.GetFromJsonAsync<CatalogProduct>($"api/products/{productId}", ct);
            if (p is null) return null;
            var price = new ProductPrice(p.Id, p.Name, p.Price, p.Currency, true);
            await UpsertAsync(price, ct);
            return price;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task UpsertAsync(ProductPrice price, CancellationToken ct)
    {
        var row = await db.ProductSnapshots.FirstOrDefaultAsync(p => p.ProductId == price.ProductId, ct);
        if (row is null) db.ProductSnapshots.Add(row = new ProductSnapshot { ProductId = price.ProductId });
        row.Name = price.Name; row.Price = price.Price; row.Currency = price.Currency; row.IsActive = price.IsActive;
        row.UpdatedOnUtc = DateTime.UtcNow;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { db.ChangeTracker.Clear(); } // lost an insert race, the other writer wins
    }

    public async Task ChangePriceAsync(Guid productId, decimal newPrice, CancellationToken ct)
    {
        var row = await db.ProductSnapshots.FirstOrDefaultAsync(p => p.ProductId == productId, ct);
        if (row is null) return; // unknown product: read-through will fetch the current price on first order
        row.Price = newPrice; row.UpdatedOnUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task DiscontinueAsync(Guid productId, CancellationToken ct)
    {
        var row = await db.ProductSnapshots.FirstOrDefaultAsync(p => p.ProductId == productId, ct);
        if (row is null) return;
        row.IsActive = false; row.UpdatedOnUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private static ProductPrice Map(ProductSnapshot r) => new(r.ProductId, r.Name, r.Price, r.Currency, r.IsActive);
}
