using ShopSphere.BuildingBlocks.Domain;
using ShopSphere.Ordering.Domain.Events;
using ShopSphere.Ordering.Domain.ValueObjects;

namespace ShopSphere.Ordering.Domain.Orders;

/// <summary>Order aggregate root. Owns its items and the status state machine:
/// Pending -> StockReserved -> Confirmed, or -> Cancelled from any non-final state.</summary>
public sealed class Order : AggregateRoot
{
    private readonly List<OrderItem> _items = new();

    public Guid UserId { get; private set; }
    public OrderStatus Status { get; private set; }
    public string Currency { get; private set; } = "EGP";
    public DateTime CreatedOnUtc { get; private set; }
    public string? CancellationReason { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public Money Total => _items.Aggregate(Money.Zero(Currency), (sum, i) => sum.Add(i.LineTotal));

    private Order() { } // EF Core

    public static Order Place(Guid userId, string currency, IEnumerable<(Guid ProductId, string Name, int Quantity, decimal Price)> lines)
    {
        var order = new Order
        {
            UserId = userId,
            Currency = currency.ToUpperInvariant(),
            Status = OrderStatus.Pending,
            CreatedOnUtc = DateTime.UtcNow
        };

        foreach (var line in lines)
        {
            order._items.Add(new OrderItem(line.ProductId, line.Name, line.Quantity, new Money(line.Price, currency)));
        }

        if (order._items.Count == 0) throw new DomainException("An order needs at least one item.");

        order.Raise(new OrderPlacedDomainEvent(order.Id));
        return order;
    }

    public void MarkStockReserved()
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException($"Cannot reserve stock for an order in status {Status}.");
        Status = OrderStatus.StockReserved;
    }

    public void Confirm()
    {
        if (Status != OrderStatus.StockReserved)
            throw new DomainException($"Cannot confirm an order in status {Status}.");
        Status = OrderStatus.Confirmed;
        Raise(new OrderConfirmedDomainEvent(Id));
    }

    public void Cancel(string reason)
    {
        if (Status is OrderStatus.Confirmed or OrderStatus.Cancelled)
            throw new DomainException($"Cannot cancel an order in status {Status}.");
        Status = OrderStatus.Cancelled;
        CancellationReason = reason;
        Raise(new OrderCancelledDomainEvent(Id, reason));
    }
}
