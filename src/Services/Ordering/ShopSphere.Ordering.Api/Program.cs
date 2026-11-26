using System.Security.Claims;
using System.Reflection;
using MediatR;
using ShopSphere.BuildingBlocks.Application;
using ShopSphere.BuildingBlocks.EventBus;
using ShopSphere.BuildingBlocks.Infrastructure;
using ShopSphere.Contracts.Events;
using ShopSphere.Ordering.Application.Orders;
using ShopSphere.Ordering.Application.Orders.Commands;
using ShopSphere.Ordering.Application.Abstractions;
using ShopSphere.Ordering.Application.Orders.IntegrationHandlers;
using ShopSphere.Ordering.Application.Orders.Queries;
using ShopSphere.Ordering.Domain.Orders;
using ShopSphere.Ordering.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddShopSphereApi(builder.Configuration);
services.AddOpenApi();
services.AddApplicationCore(typeof(PlaceOrderCommand).Assembly);
services.AddShopSphereDbContext<OrderingDbContext>(builder.Configuration);
services.AddScoped<IOrderRepository, OrderRepository>();
services.AddHttpClient<IProductPriceService, ProductPriceService>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:Catalog"] ?? "http://localhost:5102/"));
services.AddShopSphereEventBus(builder.Configuration, "ordering");
services.SubscribeIntegrationEvent<StockReservedIntegrationEvent, StockReservedHandler>();
services.SubscribeIntegrationEvent<StockReservationFailedIntegrationEvent, StockReservationFailedHandler>();
services.SubscribeIntegrationEvent<PaymentSucceededIntegrationEvent, PaymentSucceededHandler>();
services.SubscribeIntegrationEvent<PaymentFailedIntegrationEvent, PaymentFailedHandler>();
services.SubscribeIntegrationEvent<ProductCreatedIntegrationEvent, ProductCreatedPriceHandler>();
services.SubscribeIntegrationEvent<ProductPriceChangedIntegrationEvent, ProductPriceChangedPriceHandler>();
services.SubscribeIntegrationEvent<ProductDiscontinuedIntegrationEvent, ProductDiscontinuedPriceHandler>();

var app = builder.Build();
await app.EnsureDatabaseAsync<OrderingDbContext>();
app.UseShopSphereApi();
if (app.Environment.IsDevelopment()) app.MapOpenApi();

var orders = app.MapGroup("/api/orders").RequireAuthorization();
orders.MapPost("/", async (PlaceOrderRequest r, ClaimsPrincipal user, ISender mediator, CancellationToken ct) =>
{
    var id = await mediator.Send(new PlaceOrderCommand(user.GetUserId(), "EGP",
        r.Items.Select(i => new PlaceOrderLine(i.ProductId, "", i.Quantity, 0m)).ToList()), ct);
    return Results.Created($"/api/orders/{id}", new { id });
});
orders.MapGet("/", async (ClaimsPrincipal user, ISender mediator, CancellationToken ct) =>
    Results.Ok(await mediator.Send(new GetMyOrdersQuery(user.GetUserId()), ct)));
orders.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, ISender mediator, CancellationToken ct) =>
    Results.Ok(await mediator.Send(new GetOrderQuery(id, user.GetUserId()), ct)));
orders.MapPost("/{id:guid}/cancel", async (Guid id, ClaimsPrincipal user, ISender mediator, CancellationToken ct) =>
{
    await mediator.Send(new CancelOrderCommand(id, user.GetUserId(), "Cancelled by customer"), ct);
    return Results.NoContent();
});

app.Run();

public sealed record PlaceOrderItem(Guid ProductId, int Quantity);
public sealed record PlaceOrderRequest(IReadOnlyList<PlaceOrderItem> Items);
public partial class Program;
