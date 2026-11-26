using ShopSphere.BuildingBlocks.Domain;

namespace ShopSphere.Payment.Domain.Payments;

public enum PaymentStatus { Pending = 0, Succeeded = 1, Failed = 2 }

public sealed class PaymentRecord : AggregateRoot
{
    public Guid OrderId { get; private set; }
    public Guid UserId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "EGP";
    public PaymentStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    private PaymentRecord() { }

    public static PaymentRecord Create(Guid orderId, Guid userId, decimal amount, string currency)
    {
        if (amount <= 0) throw new DomainException("Payment amount must be positive.");
        return new PaymentRecord
        {
            OrderId = orderId, UserId = userId, Amount = amount, Currency = currency,
            Status = PaymentStatus.Pending, CreatedOnUtc = DateTime.UtcNow
        };
    }

    public void MarkSucceeded()
    {
        if (Status != PaymentStatus.Pending) throw new DomainException("Payment already settled.");
        Status = PaymentStatus.Succeeded;
    }

    public void MarkFailed(string reason)
    {
        if (Status != PaymentStatus.Pending) throw new DomainException("Payment already settled.");
        Status = PaymentStatus.Failed;
        FailureReason = reason;
    }
}
