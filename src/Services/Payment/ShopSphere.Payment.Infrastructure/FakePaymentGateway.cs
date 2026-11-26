using ShopSphere.Payment.Domain.Payments;

namespace ShopSphere.Payment.Infrastructure;

/// <summary>Demo gateway, no real money moves. Deterministic: amounts ending in .99 are
/// declined so the compensation path (stock release + order cancel) can be demoed.</summary>
public sealed class FakePaymentGateway : IPaymentGateway
{
    public Task<GatewayResult> ChargeAsync(Guid orderId, decimal amount, string currency, CancellationToken ct)
    {
        var declined = decimal.Round(amount - decimal.Truncate(amount), 2) == 0.99m;
        return Task.FromResult(declined ? new GatewayResult(false, "Card declined") : new GatewayResult(true, null));
    }
}
