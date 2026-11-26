namespace ShopSphere.Inventory.Domain.Stock;

public interface IStockRepository
{
    Task<StockItem?> GetByProductAsync(Guid productId, CancellationToken ct);
    Task<IReadOnlyList<StockItem>> GetByProductsAsync(IEnumerable<Guid> productIds, CancellationToken ct);
    Task AddAsync(StockItem item, CancellationToken ct);
    Task<Reservation?> GetReservationAsync(Guid orderId, CancellationToken ct);
    Task AddReservationAsync(Reservation reservation, CancellationToken ct);
}
