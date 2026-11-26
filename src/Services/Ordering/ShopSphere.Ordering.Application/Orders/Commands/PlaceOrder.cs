using FluentValidation;
using MediatR;
using ShopSphere.BuildingBlocks.Application.Abstractions;
using ShopSphere.BuildingBlocks.Application.Messaging;
using ShopSphere.Contracts;
using ShopSphere.Contracts.Events;
using ShopSphere.Ordering.Domain.Orders;

namespace ShopSphere.Ordering.Application.Orders.Commands;

public sealed record PlaceOrderLine(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);

public sealed record PlaceOrderCommand(Guid UserId, string Currency, IReadOnlyList<PlaceOrderLine> Items) : IRequest<Guid>;

public sealed class PlaceOrderValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Currency).Length(3);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(i =>
        {
            i.RuleFor(l => l.ProductId).NotEmpty();
            i.RuleFor(l => l.Quantity).GreaterThan(0);
            i.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}

/// <summary>Creates the order and starts the saga by collecting OrderCreated for the outbox.</summary>
public sealed class PlaceOrderHandler : IRequestHandler<PlaceOrderCommand, Guid>
{
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIntegrationEventCollector _events;

    public PlaceOrderHandler(IOrderRepository orders, IUnitOfWork unitOfWork, IIntegrationEventCollector events)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
        _events = events;
    }

    public async Task<Guid> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        var order = Order.Place(
            request.UserId,
            request.Currency,
            request.Items.Select(i => (i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)));

        await _orders.AddAsync(order, cancellationToken);

        _events.Collect(new OrderCreatedIntegrationEvent(
            order.Id,
            order.UserId,
            order.Items.Select(i => new OrderItemSnapshot(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice.Amount)).ToList(),
            order.Total.Amount,
            order.Currency));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return order.Id;
    }
}
