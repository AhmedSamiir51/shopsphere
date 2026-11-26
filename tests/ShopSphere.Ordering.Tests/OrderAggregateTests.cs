using FluentAssertions;
using ShopSphere.BuildingBlocks.Domain;
using ShopSphere.Ordering.Domain.Events;
using ShopSphere.Ordering.Domain.Orders;
using Xunit;

namespace ShopSphere.Ordering.Tests;

public class OrderAggregateTests
{
    private static Order NewOrder() => Order.Place(Guid.NewGuid(), "EGP", new[]
    {
        (Guid.NewGuid(), "Keyboard", 2, 500m),
        (Guid.NewGuid(), "Mouse", 1, 250m)
    });

    [Fact]
    public void Place_computes_total_and_raises_event()
    {
        var order = NewOrder();

        order.Total.Amount.Should().Be(1250m);
        order.Status.Should().Be(OrderStatus.Pending);
        order.DomainEvents.Should().ContainSingle(e => e is OrderPlacedDomainEvent);
    }

    [Fact]
    public void Place_without_items_is_rejected()
    {
        var act = () => Order.Place(Guid.NewGuid(), "EGP", Array.Empty<(Guid, string, int, decimal)>());
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Happy_path_reaches_confirmed()
    {
        var order = NewOrder();
        order.MarkStockReserved();
        order.Confirm();
        order.Status.Should().Be(OrderStatus.Confirmed);
    }

    [Fact]
    public void Cannot_confirm_before_stock_is_reserved()
    {
        var order = NewOrder();
        var act = () => order.Confirm();
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancel_records_reason_and_raises_event()
    {
        var order = NewOrder();
        order.Cancel("out of stock");
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancellationReason.Should().Be("out of stock");
        order.DomainEvents.Should().Contain(e => e is OrderCancelledDomainEvent);
    }

    [Fact]
    public void Confirmed_order_cannot_be_cancelled()
    {
        var order = NewOrder();
        order.MarkStockReserved();
        order.Confirm();
        var act = () => order.Cancel("changed mind");
        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Non_positive_quantity_is_rejected(int qty)
    {
        var act = () => Order.Place(Guid.NewGuid(), "EGP", new[] { (Guid.NewGuid(), "X", qty, 10m) });
        act.Should().Throw<DomainException>();
    }
}
