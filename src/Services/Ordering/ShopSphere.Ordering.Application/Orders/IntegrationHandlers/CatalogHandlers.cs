using ShopSphere.BuildingBlocks.Application.Messaging;
using ShopSphere.Contracts.Events;
using ShopSphere.Ordering.Application.Abstractions;

namespace ShopSphere.Ordering.Application.Orders.IntegrationHandlers;

public sealed class ProductCreatedPriceHandler(IProductPriceService prices) : IIntegrationEventHandler<ProductCreatedIntegrationEvent>
{
    public Task Handle(ProductCreatedIntegrationEvent e, CancellationToken ct) =>
        prices.UpsertAsync(new ProductPrice(e.ProductId, e.Name, e.Price, e.Currency, true), ct);
}

public sealed class ProductPriceChangedPriceHandler(IProductPriceService prices) : IIntegrationEventHandler<ProductPriceChangedIntegrationEvent>
{
    public Task Handle(ProductPriceChangedIntegrationEvent e, CancellationToken ct) =>
        prices.ChangePriceAsync(e.ProductId, e.NewPrice, ct);
}

public sealed class ProductDiscontinuedPriceHandler(IProductPriceService prices) : IIntegrationEventHandler<ProductDiscontinuedIntegrationEvent>
{
    public Task Handle(ProductDiscontinuedIntegrationEvent e, CancellationToken ct) =>
        prices.DiscontinueAsync(e.ProductId, ct);
}
