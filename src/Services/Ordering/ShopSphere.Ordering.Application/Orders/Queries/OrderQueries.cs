using MediatR;
using ShopSphere.BuildingBlocks.Domain;
using ShopSphere.Ordering.Domain.Orders;

namespace ShopSphere.Ordering.Application.Orders.Queries;

public sealed record GetOrderQuery(Guid OrderId, Guid UserId) : IRequest<OrderDto>;
public sealed record GetMyOrdersQuery(Guid UserId) : IRequest<IReadOnlyList<OrderDto>>;

public sealed class GetOrderHandler : IRequestHandler<GetOrderQuery, OrderDto>
{
    private readonly IOrderRepository _orders;
    public GetOrderHandler(IOrderRepository orders) => _orders = orders;

    public async Task<OrderDto> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null || order.UserId != request.UserId) throw new DomainException("Order not found.");
        return order.ToDto();
    }
}

public sealed class GetMyOrdersHandler : IRequestHandler<GetMyOrdersQuery, IReadOnlyList<OrderDto>>
{
    private readonly IOrderRepository _orders;
    public GetMyOrdersHandler(IOrderRepository orders) => _orders = orders;

    public async Task<IReadOnlyList<OrderDto>> Handle(GetMyOrdersQuery request, CancellationToken cancellationToken) =>
        (await _orders.GetByUserAsync(request.UserId, cancellationToken)).Select(o => o.ToDto()).ToList();
}
