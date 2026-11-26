using System.Reflection;
using MediatR;
using ShopSphere.BuildingBlocks.Application;
using ShopSphere.BuildingBlocks.EventBus;
using ShopSphere.BuildingBlocks.Infrastructure;
using ShopSphere.Contracts.Events;
using ShopSphere.Inventory.Application.Handlers;
using ShopSphere.Inventory.Domain.Stock;
using ShopSphere.Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddShopSphereApi(builder.Configuration);
services.AddOpenApi();
services.AddApplicationCore(typeof(OrderCreatedHandler).Assembly);
services.AddShopSphereDbContext<InventoryDbContext>(builder.Configuration);
services.AddScoped<IStockRepository, StockRepository>();
services.AddShopSphereEventBus(builder.Configuration, "inventory");
services.SubscribeIntegrationEvent<ProductCreatedIntegrationEvent, ProductCreatedHandler>();
services.SubscribeIntegrationEvent<OrderCreatedIntegrationEvent, OrderCreatedHandler>();
services.SubscribeIntegrationEvent<OrderCancelledIntegrationEvent, OrderCancelledHandler>();
services.SubscribeIntegrationEvent<OrderConfirmedIntegrationEvent, OrderConfirmedHandler>();

var app = builder.Build();
await app.EnsureDatabaseAsync<InventoryDbContext>();
app.UseShopSphereApi();
if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.MapGet("/api/stock/{productId:guid}", async (Guid productId, IStockRepository stock, CancellationToken ct) =>
{
    var item = await stock.GetByProductAsync(productId, ct);
    return item is null ? Results.NotFound() : Results.Ok(new { item.ProductId, item.Available, item.Reserved });
});

app.Run();

public partial class Program;
