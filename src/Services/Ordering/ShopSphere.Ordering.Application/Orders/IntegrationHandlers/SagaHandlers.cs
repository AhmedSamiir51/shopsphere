using ShopSphere.BuildingBlocks.Application.Abstractions;
using ShopSphere.BuildingBlocks.Application.Messaging;
using ShopSphere.Contracts.Events;
using ShopSphere.Ordering.Domain.Orders;

namespace ShopSphere.Ordering.Application.Orders.IntegrationHandlers;

/// <summary>Stock held: move the order forward and ask Payment to charge.</summary>
public sealed class StockReservedHandler : IIntegrationEventHandler<StockReservedIntegrationEvent>
{
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _uow;
    private readonly IIntegrationEventCollector _events;

    public StockReservedHandler(IOrderRepository orders, IUnitOfWork uow, IIntegrationEventCollector events)
    {
        _orders = orders; _uow = uow; _events = events;
    }

    public async Task Handle(StockReservedIntegrationEvent e, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(e.OrderId, ct);
        if (order is null) return;
        order.MarkStockReserved();
        _events.Collect(new PaymentRequestedIntegrationEvent(order.Id, order.UserId, order.Total.Amount, order.Currency));
        await _uow.SaveChangesAsync(ct);
    }
}

/// <summary>Out of stock: cancel the order (nothing to compensate yet).</summary>
public sealed class StockReservationFailedHandler : IIntegrationEventHandler<StockReservationFailedIntegrationEvent>
{
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _uow;
    private readonly IIntegrationEventCollector _events;

    public StockReservationFailedHandler(IOrderRepository orders, IUnitOfWork uow, IIntegrationEventCollector events)
    {
        _orders = orders; _uow = uow; _events = events;
    }

    public async Task Handle(StockReservationFailedIntegrationEvent e, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(e.OrderId, ct);
        if (order is null) return;
        order.Cancel(e.Reason);
        _events.Collect(new OrderCancelledIntegrationEvent(order.Id, order.UserId, e.Reason));
        await _uow.SaveChangesAsync(ct);
    }
}

/// <summary>Paid: confirm the order and tell everyone.</summary>
public sealed class PaymentSucceededHandler : IIntegrationEventHandler<PaymentSucceededIntegrationEvent>
{
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _uow;
    private readonly IIntegrationEventCollector _events;

    public PaymentSucceededHandler(IOrderRepository orders, IUnitOfWork uow, IIntegrationEventCollector events)
    {
        _orders = orders; _uow = uow; _events = events;
    }

    public async Task Handle(PaymentSucceededIntegrationEvent e, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(e.OrderId, ct);
        if (order is null) return;
        order.Confirm();
        _events.Collect(new OrderConfirmedIntegrationEvent(order.Id, order.UserId, order.Total.Amount, order.Currency));
        await _uow.SaveChangesAsync(ct);
    }
}

/// <summary>Payment failed: cancel. Inventory listens to OrderCancelled and releases the stock (compensation).</summary>
public sealed class PaymentFailedHandler : IIntegrationEventHandler<PaymentFailedIntegrationEvent>
{
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _uow;
    private readonly IIntegrationEventCollector _events;

    public PaymentFailedHandler(IOrderRepository orders, IUnitOfWork uow, IIntegrationEventCollector events)
    {
        _orders = orders; _uow = uow; _events = events;
    }

    public async Task Handle(PaymentFailedIntegrationEvent e, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(e.OrderId, ct);
        if (order is null) return;
        order.Cancel($"Payment failed: {e.Reason}");
        _events.Collect(new OrderCancelledIntegrationEvent(order.Id, order.UserId, $"Payment failed: {e.Reason}"));
        await _uow.SaveChangesAsync(ct);
    }
}
