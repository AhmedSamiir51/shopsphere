namespace ShopSphere.Ordering.Application.Orders;

public sealed record OrderItemDto(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);

public sealed record OrderDto(
    Guid Id,
    Guid UserId,
    string Status,
    decimal Total,
    string Currency,
    DateTime CreatedOnUtc,
    string? CancellationReason,
    IReadOnlyList<OrderItemDto> Items);
