using ShopSphere.Ordering.Domain.Orders;

namespace ShopSphere.Ordering.Application.Orders;

internal static class OrderMapping
{
    public static OrderDto ToDto(this Order o) => new(
        o.Id, o.UserId, o.Status.ToString(), o.Total.Amount, o.Currency, o.CreatedOnUtc, o.CancellationReason,
        o.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice.Amount)).ToList());
}
