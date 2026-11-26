namespace ShopSphere.Payment.Domain.Payments;

public interface IPaymentRepository
{
    Task<PaymentRecord?> GetByOrderAsync(Guid orderId, CancellationToken ct);
    Task AddAsync(PaymentRecord payment, CancellationToken ct);
}

/// <summary>Port to a payment provider. The demo adapter is a deterministic fake gateway.</summary>
public interface IPaymentGateway
{
    Task<GatewayResult> ChargeAsync(Guid orderId, decimal amount, string currency, CancellationToken ct);
}

public sealed record GatewayResult(bool Success, string? FailureReason);
