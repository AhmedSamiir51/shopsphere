using MediatR;
using ShopSphere.BuildingBlocks.Application.Messaging;
using ShopSphere.Cart.Domain.Carts;
using ShopSphere.Contracts.Events;

namespace ShopSphere.Cart.Application.Carts;

public sealed record CartDto(Guid UserId, IReadOnlyList<CartLine> Lines, decimal Total);

public sealed record GetCartQuery(Guid UserId) : IRequest<CartDto>;
public sealed record AddToCartCommand(Guid UserId, Guid ProductId, string ProductName, decimal UnitPrice, string ImageUrl, int Quantity) : IRequest<CartDto>;
public sealed record SetQuantityCommand(Guid UserId, Guid ProductId, int Quantity) : IRequest<CartDto>;
public sealed record RemoveFromCartCommand(Guid UserId, Guid ProductId) : IRequest<CartDto>;
public sealed record ClearCartCommand(Guid UserId) : IRequest;

public sealed class CartHandlers :
    IRequestHandler<GetCartQuery, CartDto>,
    IRequestHandler<AddToCartCommand, CartDto>,
    IRequestHandler<SetQuantityCommand, CartDto>,
    IRequestHandler<RemoveFromCartCommand, CartDto>,
    IRequestHandler<ClearCartCommand>
{
    private readonly ICartRepository _carts;
    public CartHandlers(ICartRepository carts) => _carts = carts;

    private static CartDto Map(ShoppingCart c) => new(c.UserId, c.Lines, c.Total);

    public async Task<CartDto> Handle(GetCartQuery q, CancellationToken ct) => Map(await _carts.GetAsync(q.UserId, ct));

    public async Task<CartDto> Handle(AddToCartCommand c, CancellationToken ct)
    {
        var cart = await _carts.GetAsync(c.UserId, ct);
        cart.AddOrIncrease(new CartLine(c.ProductId, c.ProductName, c.UnitPrice, c.ImageUrl, c.Quantity));
        await _carts.SaveAsync(cart, ct);
        return Map(cart);
    }

    public async Task<CartDto> Handle(SetQuantityCommand c, CancellationToken ct)
    {
        var cart = await _carts.GetAsync(c.UserId, ct);
        cart.SetQuantity(c.ProductId, c.Quantity);
        await _carts.SaveAsync(cart, ct);
        return Map(cart);
    }

    public async Task<CartDto> Handle(RemoveFromCartCommand c, CancellationToken ct)
    {
        var cart = await _carts.GetAsync(c.UserId, ct);
        cart.Remove(c.ProductId);
        await _carts.SaveAsync(cart, ct);
        return Map(cart);
    }

    public Task Handle(ClearCartCommand c, CancellationToken ct) => _carts.DeleteAsync(c.UserId, ct);
}

/// <summary>Order placed: the basket is done, empty it.</summary>
public sealed class OrderCreatedClearsCart : IIntegrationEventHandler<OrderCreatedIntegrationEvent>
{
    private readonly ICartRepository _carts;
    public OrderCreatedClearsCart(ICartRepository carts) => _carts = carts;
    public Task Handle(OrderCreatedIntegrationEvent e, CancellationToken ct) => _carts.DeleteAsync(e.UserId, ct);
}
