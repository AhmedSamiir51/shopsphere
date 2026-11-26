using FluentValidation;
using MediatR;
using ShopSphere.BuildingBlocks.Application.Abstractions;
using ShopSphere.BuildingBlocks.Application.Messaging;
using ShopSphere.BuildingBlocks.Domain;
using ShopSphere.Catalog.Domain.Products;
using ShopSphere.Contracts.Events;

namespace ShopSphere.Catalog.Application.Products;

public sealed record ProductDto(Guid Id, string Name, string Sku, string Description, decimal Price, string Currency,
    string ImageUrl, Guid CategoryId, double Rating, int ReviewCount);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

public sealed record SearchProductsQuery(string? Query, Guid? CategoryId, decimal? MinPrice, decimal? MaxPrice,
    string Sort = "relevance", int Page = 1, int PageSize = 12) : IRequest<PagedResult<ProductDto>>;

public sealed record GetProductQuery(Guid Id) : IRequest<ProductDto>;

public sealed record CreateProductCommand(string Name, string Sku, string Description, decimal Price, string Currency,
    string ImageUrl, Guid CategoryId) : IRequest<Guid>;

public sealed record ChangePriceCommand(Guid ProductId, decimal NewPrice) : IRequest;

public sealed record DiscontinueProductCommand(Guid ProductId) : IRequest;

public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Currency).Length(3);
    }
}

internal static class ProductMapping
{
    public static ProductDto ToDto(this Product p) => new(p.Id, p.Name, p.Sku, p.Description, p.Price, p.Currency,
        p.ImageUrl, p.CategoryId, p.Rating, p.ReviewCount);
}

public sealed class SearchProductsHandler : IRequestHandler<SearchProductsQuery, PagedResult<ProductDto>>
{
    private readonly IProductRepository _products;
    private readonly ICatalogCache _cache;
    public SearchProductsHandler(IProductRepository products, ICatalogCache cache) { _products = products; _cache = cache; }

    public async Task<PagedResult<ProductDto>> Handle(SearchProductsQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page);
        var size = Math.Clamp(q.PageSize, 1, 60);
        var key = $"search:{q.Query?.Trim().ToLowerInvariant()}|{q.CategoryId}|{q.MinPrice}|{q.MaxPrice}|{q.Sort}|{page}|{size}";
        var cached = await _cache.GetAsync<PagedResult<ProductDto>>(key, ct);
        if (cached is not null) return cached;

        var (items, total) = await _products.SearchAsync(q.Query, q.CategoryId, q.MinPrice, q.MaxPrice, q.Sort, page, size, ct);
        var result = new PagedResult<ProductDto>(items.Select(i => i.ToDto()).ToList(), total, page, size);
        await _cache.SetAsync(key, result, TimeSpan.FromMinutes(5), ct);
        return result;
    }
}

public sealed class GetProductHandler : IRequestHandler<GetProductQuery, ProductDto>
{
    private readonly IProductRepository _products;
    private readonly ICatalogCache _cache;
    public GetProductHandler(IProductRepository products, ICatalogCache cache) { _products = products; _cache = cache; }

    public async Task<ProductDto> Handle(GetProductQuery q, CancellationToken ct)
    {
        var key = $"product:{q.Id}";
        var cached = await _cache.GetAsync<ProductDto>(key, ct);
        if (cached is not null) return cached;
        var dto = (await _products.GetAsync(q.Id, ct) ?? throw new DomainException("Product not found.")).ToDto();
        await _cache.SetAsync(key, dto, TimeSpan.FromMinutes(10), ct);
        return dto;
    }
}

public sealed class CreateProductHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _uow;
    private readonly IIntegrationEventCollector _events;
    private readonly ICatalogCache _cache;

    public CreateProductHandler(IProductRepository products, IUnitOfWork uow, IIntegrationEventCollector events, ICatalogCache cache)
    {
        _products = products; _uow = uow; _events = events; _cache = cache;
    }

    public async Task<Guid> Handle(CreateProductCommand c, CancellationToken ct)
    {
        var product = Product.Create(c.Name, c.Sku, c.Description, c.Price, c.Currency, c.ImageUrl, c.CategoryId);
        await _products.AddAsync(product, ct);
        _events.Collect(new ProductCreatedIntegrationEvent(product.Id, product.Name, product.Sku, product.Price, product.Currency, product.CategoryId));
        await _uow.SaveChangesAsync(ct);
        await _cache.InvalidateAsync(ct);
        return product.Id;
    }
}

public sealed class ChangePriceHandler : IRequestHandler<ChangePriceCommand>
{
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _uow;
    private readonly IIntegrationEventCollector _events;
    private readonly ICatalogCache _cache;

    public ChangePriceHandler(IProductRepository products, IUnitOfWork uow, IIntegrationEventCollector events, ICatalogCache cache)
    {
        _products = products; _uow = uow; _events = events; _cache = cache;
    }

    public async Task Handle(ChangePriceCommand c, CancellationToken ct)
    {
        var product = await _products.GetAsync(c.ProductId, ct) ?? throw new DomainException("Product not found.");
        var old = product.Price;
        product.ChangePrice(c.NewPrice);
        if (product.Price != old)
            _events.Collect(new ProductPriceChangedIntegrationEvent(product.Id, old, product.Price, product.Currency));
        await _uow.SaveChangesAsync(ct);
        await _cache.InvalidateAsync(ct);
    }
}

public sealed class DiscontinueProductHandler : IRequestHandler<DiscontinueProductCommand>
{
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _uow;
    private readonly IIntegrationEventCollector _events;
    private readonly ICatalogCache _cache;

    public DiscontinueProductHandler(IProductRepository products, IUnitOfWork uow, IIntegrationEventCollector events, ICatalogCache cache)
    {
        _products = products; _uow = uow; _events = events; _cache = cache;
    }

    public async Task Handle(DiscontinueProductCommand c, CancellationToken ct)
    {
        var product = await _products.GetAsync(c.ProductId, ct) ?? throw new DomainException("Product not found.");
        product.Discontinue();
        _events.Collect(new ProductDiscontinuedIntegrationEvent(product.Id));
        await _uow.SaveChangesAsync(ct);
        await _cache.InvalidateAsync(ct);
    }
}
