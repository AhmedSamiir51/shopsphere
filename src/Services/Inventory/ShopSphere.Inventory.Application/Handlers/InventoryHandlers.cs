using ShopSphere.BuildingBlocks.Application.Abstractions;
using ShopSphere.BuildingBlocks.Application.Messaging;
using ShopSphere.Contracts.Events;
using ShopSphere.Inventory.Domain.Stock;

namespace ShopSphere.Inventory.Application.Handlers;

/// <summary>All-or-nothing reservation for an order. Idempotent per order id.</summary>
public sealed class OrderCreatedHandler : IIntegrationEventHandler<OrderCreatedIntegrationEvent>
{
    private readonly IStockRepository _stock;
    private readonly IUnitOfWork _uow;
    private readonly IIntegrationEventCollector _events;

    public OrderCreatedHandler(IStockRepository stock, IUnitOfWork uow, IIntegrationEventCollector events)
    {
        _stock = stock; _uow = uow; _events = events;
    }

    public async Task Handle(OrderCreatedIntegrationEvent e, CancellationToken ct)
    {
        if (await _stock.GetReservationAsync(e.OrderId, ct) is not null) return;

        var wanted = e.Items.GroupBy(i => i.ProductId).ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));
        var items = (await _stock.GetByProductsAsync(wanted.Keys, ct)).ToDictionary(s => s.ProductId);

        var missing = wanted.Where(w => !items.TryGetValue(w.Key, out var s) || !s.CanReserve(w.Value))
            .Select(w => w.Key).ToList();

        if (missing.Count > 0)
        {
            _events.Collect(new StockReservationFailedIntegrationEvent(e.OrderId, "Insufficient stock", missing));
            await _uow.SaveChangesAsync(ct);
            return;
        }

        foreach (var (productId, qty) in wanted) items[productId].Reserve(qty);

        await _stock.AddReservationAsync(
            Reservation.Create(e.OrderId, wanted.Select(w => new ReservationLine(w.Key, w.Value))), ct);
        _events.Collect(new StockReservedIntegrationEvent(e.OrderId, e.Items));
        await _uow.SaveChangesAsync(ct);
    }
}

/// <summary>Compensation: order cancelled after reservation, put the units back.</summary>
public sealed class OrderCancelledHandler : IIntegrationEventHandler<OrderCancelledIntegrationEvent>
{
    private readonly IStockRepository _stock;
    private readonly IUnitOfWork _uow;

    public OrderCancelledHandler(IStockRepository stock, IUnitOfWork uow) { _stock = stock; _uow = uow; }

    public async Task Handle(OrderCancelledIntegrationEvent e, CancellationToken ct)
    {
        var reservation = await _stock.GetReservationAsync(e.OrderId, ct);
        if (reservation is null || reservation.Status != ReservationStatus.Active) return;

        var items = (await _stock.GetByProductsAsync(reservation.Lines.Select(l => l.ProductId), ct))
            .ToDictionary(s => s.ProductId);
        foreach (var line in reservation.Lines) items[line.ProductId].Release(line.Quantity);

        reservation.MarkReleased();
        await _uow.SaveChangesAsync(ct);
    }
}

/// <summary>Order confirmed: reserved units leave the warehouse for good.</summary>
public sealed class OrderConfirmedHandler : IIntegrationEventHandler<OrderConfirmedIntegrationEvent>
{
    private readonly IStockRepository _stock;
    private readonly IUnitOfWork _uow;

    public OrderConfirmedHandler(IStockRepository stock, IUnitOfWork uow) { _stock = stock; _uow = uow; }

    public async Task Handle(OrderConfirmedIntegrationEvent e, CancellationToken ct)
    {
        var reservation = await _stock.GetReservationAsync(e.OrderId, ct);
        if (reservation is null || reservation.Status != ReservationStatus.Active) return;

        var items = (await _stock.GetByProductsAsync(reservation.Lines.Select(l => l.ProductId), ct))
            .ToDictionary(s => s.ProductId);
        foreach (var line in reservation.Lines) items[line.ProductId].Commit(line.Quantity);

        reservation.MarkCommitted();
        await _uow.SaveChangesAsync(ct);
    }
}

/// <summary>New catalog product: start tracking stock (seeded with a demo quantity).</summary>
public sealed class ProductCreatedHandler : IIntegrationEventHandler<ProductCreatedIntegrationEvent>
{
    private const int InitialDemoStock = 100;
    private readonly IStockRepository _stock;
    private readonly IUnitOfWork _uow;

    public ProductCreatedHandler(IStockRepository stock, IUnitOfWork uow) { _stock = stock; _uow = uow; }

    public async Task Handle(ProductCreatedIntegrationEvent e, CancellationToken ct)
    {
        if (await _stock.GetByProductAsync(e.ProductId, ct) is not null) return;
        await _stock.AddAsync(StockItem.Create(e.ProductId, InitialDemoStock), ct);
        await _uow.SaveChangesAsync(ct);
    }
}
