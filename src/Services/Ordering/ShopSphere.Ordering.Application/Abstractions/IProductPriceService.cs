namespace ShopSphere.Ordering.Application.Abstractions;

/// <summary>What Ordering knows about a product. Prices are always taken from here, never from the client.</summary>
public sealed record ProductPrice(Guid ProductId, string Name, decimal Price, string Currency, bool IsActive);

/// <summary>Ordering's local read-model of Catalog. It is kept fresh by Catalog's integration events
/// (ProductCreated, PriceChanged, Discontinued) and falls back to a Catalog HTTP read when a product
/// is unknown (for example the event was published before this service's queue existed).</summary>
public interface IProductPriceService
{
    Task<ProductPrice?> GetAsync(Guid productId, CancellationToken ct);
    Task UpsertAsync(ProductPrice price, CancellationToken ct);
    Task DiscontinueAsync(Guid productId, CancellationToken ct);
    Task ChangePriceAsync(Guid productId, decimal newPrice, CancellationToken ct);
}
