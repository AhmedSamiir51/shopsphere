using System.Reflection;
using MediatR;
using ShopSphere.BuildingBlocks.Application;
using ShopSphere.BuildingBlocks.EventBus;
using ShopSphere.BuildingBlocks.Infrastructure;
using System.Security.Claims;
using ShopSphere.Cart.Application.Carts;
using ShopSphere.Cart.Domain.Carts;
using ShopSphere.Cart.Infrastructure.Persistence;
using ShopSphere.Contracts.Events;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddShopSphereApi(builder.Configuration);
services.AddOpenApi();
services.AddApplicationCore(typeof(GetCartQuery).Assembly);
services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379,abortConnect=false"));
services.AddSingleton<ICartRepository, RedisCartRepository>();

// Cart has no SQL database, so it uses the bus without the outbox; the inbox store still needs Postgres.
services.AddShopSphereEventBus(builder.Configuration, "cart");
services.SubscribeIntegrationEvent<OrderCreatedIntegrationEvent, OrderCreatedClearsCart>();

var app = builder.Build();
app.UseShopSphereApi();
if (app.Environment.IsDevelopment()) app.MapOpenApi();

var cart = app.MapGroup("/api/cart").RequireAuthorization();
cart.MapGet("/", async (ClaimsPrincipal u, ISender m, CancellationToken ct) => Results.Ok(await m.Send(new GetCartQuery(u.GetUserId()), ct)));
cart.MapPost("/items", async (AddItemBody b, ClaimsPrincipal u, ISender m, CancellationToken ct) =>
    Results.Ok(await m.Send(new AddToCartCommand(u.GetUserId(), b.ProductId, b.ProductName, b.UnitPrice, b.ImageUrl ?? "", b.Quantity), ct)));
cart.MapPut("/items/{productId:guid}", async (Guid productId, QuantityBody b, ClaimsPrincipal u, ISender m, CancellationToken ct) =>
    Results.Ok(await m.Send(new SetQuantityCommand(u.GetUserId(), productId, b.Quantity), ct)));
cart.MapDelete("/items/{productId:guid}", async (Guid productId, ClaimsPrincipal u, ISender m, CancellationToken ct) =>
    Results.Ok(await m.Send(new RemoveFromCartCommand(u.GetUserId(), productId), ct)));
cart.MapDelete("/", async (ClaimsPrincipal u, ISender m, CancellationToken ct) =>
{ await m.Send(new ClearCartCommand(u.GetUserId()), ct); return Results.NoContent(); });

app.Run();

public sealed record AddItemBody(Guid ProductId, string ProductName, decimal UnitPrice, string? ImageUrl, int Quantity);
public sealed record QuantityBody(int Quantity);
public partial class Program;
