using Microsoft.EntityFrameworkCore;
using ShopSphere.Catalog.Domain.Products;

namespace ShopSphere.Catalog.Infrastructure.Persistence;

public sealed class ProductRepository : IProductRepository
{
    private readonly CatalogDbContext _db;
    public ProductRepository(CatalogDbContext db) => _db = db;

    public Task<Product?> GetAsync(Guid id, CancellationToken ct) => _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task AddAsync(Product product, CancellationToken ct) => await _db.Products.AddAsync(product, ct);

    public async Task<(IReadOnlyList<Product> Items, int Total)> SearchAsync(string? query, Guid? categoryId,
        decimal? minPrice, decimal? maxPrice, string sort, int page, int pageSize, CancellationToken ct)
    {
        var q = _db.Products.AsNoTracking().Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = $"%{query.Trim()}%";
            q = q.Where(p => EF.Functions.ILike(p.Name, pattern) || EF.Functions.ILike(p.Description, pattern));
        }
        if (categoryId is not null) q = q.Where(p => p.CategoryId == categoryId);
        if (minPrice is not null) q = q.Where(p => p.Price >= minPrice);
        if (maxPrice is not null) q = q.Where(p => p.Price <= maxPrice);

        q = sort switch
        {
            "price-asc" => q.OrderBy(p => p.Price),
            "price-desc" => q.OrderByDescending(p => p.Price),
            "rating" => q.OrderByDescending(p => p.Rating),
            _ => q.OrderByDescending(p => p.ReviewCount)
        };

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken ct) =>
        await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);
}
