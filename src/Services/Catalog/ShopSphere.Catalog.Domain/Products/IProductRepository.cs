namespace ShopSphere.Catalog.Domain.Products;

public interface IProductRepository
{
    Task<Product?> GetAsync(Guid id, CancellationToken ct);
    Task AddAsync(Product product, CancellationToken ct);
    Task<(IReadOnlyList<Product> Items, int Total)> SearchAsync(string? query, Guid? categoryId, decimal? minPrice,
        decimal? maxPrice, string sort, int page, int pageSize, CancellationToken ct);
    Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken ct);
}
