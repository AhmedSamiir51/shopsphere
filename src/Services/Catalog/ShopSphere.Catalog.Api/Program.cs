using System.Reflection;
using MediatR;
using ShopSphere.BuildingBlocks.Application;
using ShopSphere.BuildingBlocks.EventBus;
using ShopSphere.BuildingBlocks.Infrastructure;
using System.Security.Claims;
using ShopSphere.Catalog.Application.Products;
using ShopSphere.Catalog.Domain.Products;
using ShopSphere.Catalog.Infrastructure.Persistence;
using ShopSphere.Catalog.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddShopSphereApi(builder.Configuration);
services.AddOpenApi();
services.AddApplicationCore(typeof(SearchProductsQuery).Assembly);
services.AddShopSphereDbContext<CatalogDbContext>(builder.Configuration);
services.AddScoped<IProductRepository, ProductRepository>();
services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_ => StackExchange.Redis.ConnectionMultiplexer.Connect(
    builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379,abortConnect=false"));
services.AddSingleton<ICatalogCache, RedisCatalogCache>();
services.AddShopSphereEventBus(builder.Configuration, "catalog");

var app = builder.Build();
await app.EnsureDatabaseAsync<CatalogDbContext>();
await CatalogSeeder.SeedAsync(app.Services);
app.UseShopSphereApi();
if (app.Environment.IsDevelopment()) app.MapOpenApi();

var api = app.MapGroup("/api");
api.MapGet("/products", async (string? q, Guid? categoryId, decimal? minPrice, decimal? maxPrice, string? sort, int? page, int? pageSize, ISender m, CancellationToken ct) =>
    Results.Ok(await m.Send(new SearchProductsQuery(q, categoryId, minPrice, maxPrice, sort ?? "relevance", page ?? 1, pageSize ?? 12), ct)));
api.MapGet("/products/{id:guid}", async (Guid id, ISender m, CancellationToken ct) => Results.Ok(await m.Send(new GetProductQuery(id), ct)));
api.MapGet("/categories", async (IProductRepository repo, CancellationToken ct) =>
    Results.Ok((await repo.GetCategoriesAsync(ct)).Select(c => new { c.Id, c.Name, c.Slug })));
api.MapPost("/products", async (CreateProductCommand c, ISender m, CancellationToken ct) =>
    Results.Created("/api/products", new { id = await m.Send(c, ct) })).RequireAuthorization(p => p.RequireRole("Admin"));
api.MapPut("/products/{id:guid}/price", async (Guid id, ChangePriceBody b, ISender m, CancellationToken ct) =>
{ await m.Send(new ChangePriceCommand(id, b.NewPrice), ct); return Results.NoContent(); }).RequireAuthorization(p => p.RequireRole("Admin"));

api.MapDelete("/products/{id:guid}", async (Guid id, ISender m, CancellationToken ct) =>
{ await m.Send(new DiscontinueProductCommand(id), ct); return Results.NoContent(); }).RequireAuthorization(p => p.RequireRole("Admin"));

app.Run();

public sealed record ChangePriceBody(decimal NewPrice);
public partial class Program;
