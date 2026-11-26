using Microsoft.EntityFrameworkCore;
using ShopSphere.Inventory.Domain.Stock;

namespace ShopSphere.Inventory.Infrastructure.Persistence;

public sealed class StockRepository : IStockRepository
{
    private readonly InventoryDbContext _db;
    public StockRepository(InventoryDbContext db) => _db = db;

    public Task<StockItem?> GetByProductAsync(Guid productId, CancellationToken ct) =>
        _db.StockItems.FirstOrDefaultAsync(s => s.ProductId == productId, ct);

    public async Task<IReadOnlyList<StockItem>> GetByProductsAsync(IEnumerable<Guid> productIds, CancellationToken ct)
    {
        var ids = productIds.ToList();
        return await _db.StockItems.Where(s => ids.Contains(s.ProductId)).ToListAsync(ct);
    }

    public async Task AddAsync(StockItem item, CancellationToken ct) => await _db.StockItems.AddAsync(item, ct);

    public Task<Reservation?> GetReservationAsync(Guid orderId, CancellationToken ct) =>
        _db.Reservations.Include(r => r.Lines).FirstOrDefaultAsync(r => r.OrderId == orderId, ct);

    public async Task AddReservationAsync(Reservation reservation, CancellationToken ct) =>
        await _db.Reservations.AddAsync(reservation, ct);
}
