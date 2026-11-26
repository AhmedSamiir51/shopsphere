using Microsoft.EntityFrameworkCore;
using ShopSphere.Payment.Domain.Payments;

namespace ShopSphere.Payment.Infrastructure.Persistence;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _db;
    public PaymentRepository(PaymentDbContext db) => _db = db;

    public Task<PaymentRecord?> GetByOrderAsync(Guid orderId, CancellationToken ct) =>
        _db.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId, ct);

    public async Task AddAsync(PaymentRecord payment, CancellationToken ct) => await _db.Payments.AddAsync(payment, ct);
}
