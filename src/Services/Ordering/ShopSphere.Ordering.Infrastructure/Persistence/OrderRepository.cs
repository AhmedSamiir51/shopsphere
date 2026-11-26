using Microsoft.EntityFrameworkCore;
using ShopSphere.Ordering.Domain.Orders;

namespace ShopSphere.Ordering.Infrastructure.Persistence;

public sealed class OrderRepository : IOrderRepository
{
    private readonly OrderingDbContext _db;
    public OrderRepository(OrderingDbContext db) => _db = db;

    public async Task AddAsync(Order order, CancellationToken ct) => await _db.Orders.AddAsync(order, ct);

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IReadOnlyList<Order>> GetByUserAsync(Guid userId, CancellationToken ct) =>
        await _db.Orders.Include(o => o.Items).Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedOnUtc).ToListAsync(ct);
}
