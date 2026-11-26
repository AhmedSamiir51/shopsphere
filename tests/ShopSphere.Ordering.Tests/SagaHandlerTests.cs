using FluentAssertions;
using NSubstitute;
using ShopSphere.BuildingBlocks.Application.Abstractions;
using ShopSphere.BuildingBlocks.Application.Messaging;
using ShopSphere.Contracts;
using ShopSphere.Contracts.Events;
using ShopSphere.Ordering.Application.Orders.IntegrationHandlers;
using ShopSphere.Ordering.Domain.Orders;
using Xunit;

namespace ShopSphere.Ordering.Tests;

public class SagaHandlerTests
{
    private readonly IOrderRepository _repo = Substitute.For<IOrderRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IIntegrationEventCollector _events = Substitute.For<IIntegrationEventCollector>();

    private Order PendingOrder()
    {
        var order = Order.Place(Guid.NewGuid(), "EGP", new[] { (Guid.NewGuid(), "Item", 1, 100m) });
        _repo.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        return order;
    }

    [Fact]
    public async Task StockReserved_requests_payment()
    {
        var order = PendingOrder();
        var handler = new StockReservedHandler(_repo, _uow, _events);

        await handler.Handle(new StockReservedIntegrationEvent(order.Id, Array.Empty<OrderItemSnapshot>()), default);

        order.Status.Should().Be(OrderStatus.StockReserved);
        _events.Received(1).Collect(Arg.Is<IntegrationEvent>(e => e is PaymentRequestedIntegrationEvent));
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StockFailed_cancels_order()
    {
        var order = PendingOrder();
        var handler = new StockReservationFailedHandler(_repo, _uow, _events);

        await handler.Handle(new StockReservationFailedIntegrationEvent(order.Id, "no stock", Array.Empty<Guid>()), default);

        order.Status.Should().Be(OrderStatus.Cancelled);
        _events.Received(1).Collect(Arg.Is<IntegrationEvent>(e => e is OrderCancelledIntegrationEvent));
    }

    [Fact]
    public async Task PaymentSucceeded_confirms_order()
    {
        var order = PendingOrder();
        order.MarkStockReserved();
        var handler = new PaymentSucceededHandler(_repo, _uow, _events);

        await handler.Handle(new PaymentSucceededIntegrationEvent(order.Id, Guid.NewGuid(), 100m, "EGP"), default);

        order.Status.Should().Be(OrderStatus.Confirmed);
        _events.Received(1).Collect(Arg.Is<IntegrationEvent>(e => e is OrderConfirmedIntegrationEvent));
    }

    [Fact]
    public async Task PaymentFailed_cancels_order_so_inventory_can_compensate()
    {
        var order = PendingOrder();
        order.MarkStockReserved();
        var handler = new PaymentFailedHandler(_repo, _uow, _events);

        await handler.Handle(new PaymentFailedIntegrationEvent(order.Id, "card declined"), default);

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancellationReason.Should().Contain("card declined");
    }
}
