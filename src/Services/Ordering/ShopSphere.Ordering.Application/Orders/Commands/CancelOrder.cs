using MediatR;
using ShopSphere.BuildingBlocks.Application.Abstractions;
using ShopSphere.BuildingBlocks.Application.Messaging;
using ShopSphere.BuildingBlocks.Domain;
using ShopSphere.Contracts.Events;
using ShopSphere.Ordering.Domain.Orders;

namespace ShopSphere.Ordering.Application.Orders.Commands;

public sealed record CancelOrderCommand(Guid OrderId, Guid UserId, string Reason) : IRequest;

public sealed class CancelOrderHandler : IRequestHandler<CancelOrderCommand>
{
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIntegrationEventCollector _events;

    public CancelOrderHandler(IOrderRepository orders, IUnitOfWork unitOfWork, IIntegrationEventCollector events)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
        _events = events;
    }

    public async Task Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new DomainException("Order not found.");
        if (order.UserId != request.UserId) throw new DomainException("Order not found.");

        order.Cancel(request.Reason);
        _events.Collect(new OrderCancelledIntegrationEvent(order.Id, order.UserId, request.Reason));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
