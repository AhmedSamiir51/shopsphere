namespace ShopSphere.Contracts;

/// <summary>
/// Base type for every integration event that crosses a service boundary.
/// Integration events are the public, versioned contract between services -
/// unlike domain events they never expose domain internals.
/// </summary>
public abstract record IntegrationEvent
{
    /// <summary>Unique id of this event occurrence. Also used as the inbox idempotency key.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>UTC timestamp of when the event occurred.</summary>
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>A line item snapshot carried by order-related integration events.</summary>
public sealed record OrderItemSnapshot(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);
