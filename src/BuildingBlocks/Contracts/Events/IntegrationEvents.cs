namespace ShopSphere.Contracts.Events;

// ---------------------------------------------------------------------------
// Identity
// ---------------------------------------------------------------------------

public sealed record UserRegisteredIntegrationEvent(
    Guid UserId,
    string Email,
    string UserName) : IntegrationEvent;

// ---------------------------------------------------------------------------
// Catalog
// ---------------------------------------------------------------------------

public sealed record ProductCreatedIntegrationEvent(
    Guid ProductId,
    string Name,
    string Sku,
    decimal Price,
    string Currency,
    Guid CategoryId) : IntegrationEvent;

public sealed record ProductPriceChangedIntegrationEvent(
    Guid ProductId,
    decimal OldPrice,
    decimal NewPrice,
    string Currency) : IntegrationEvent;

public sealed record ProductDiscontinuedIntegrationEvent(Guid ProductId) : IntegrationEvent;

// ---------------------------------------------------------------------------
// Ordering  (saga commands/facts produced by the Ordering service)
// ---------------------------------------------------------------------------

/// <summary>Published when a customer checks out. Starts the order saga.</summary>
public sealed record OrderCreatedIntegrationEvent(
    Guid OrderId,
    Guid UserId,
    IReadOnlyList<OrderItemSnapshot> Items,
    decimal TotalAmount,
    string Currency) : IntegrationEvent;

public sealed record OrderConfirmedIntegrationEvent(
    Guid OrderId,
    Guid UserId,
    decimal TotalAmount,
    string Currency) : IntegrationEvent;

public sealed record OrderCancelledIntegrationEvent(
    Guid OrderId,
    Guid UserId,
    string Reason) : IntegrationEvent;

// ---------------------------------------------------------------------------
// Inventory
// ---------------------------------------------------------------------------

public sealed record StockReservedIntegrationEvent(
    Guid OrderId,
    IReadOnlyList<OrderItemSnapshot> Items) : IntegrationEvent;

public sealed record StockReservationFailedIntegrationEvent(
    Guid OrderId,
    string Reason,
    IReadOnlyList<Guid> OutOfStockProductIds) : IntegrationEvent;

// ---------------------------------------------------------------------------
// Payment
// ---------------------------------------------------------------------------

public sealed record PaymentRequestedIntegrationEvent(
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    string Currency) : IntegrationEvent;

public sealed record PaymentSucceededIntegrationEvent(
    Guid OrderId,
    Guid PaymentId,
    decimal Amount,
    string Currency) : IntegrationEvent;

public sealed record PaymentFailedIntegrationEvent(
    Guid OrderId,
    string Reason) : IntegrationEvent;
