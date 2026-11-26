using System.Reflection;
using MediatR;
using ShopSphere.BuildingBlocks.Application;
using ShopSphere.BuildingBlocks.EventBus;
using ShopSphere.BuildingBlocks.Infrastructure;
using ShopSphere.Contracts.Events;
using ShopSphere.Payment.Application.Handlers;
using ShopSphere.Payment.Domain.Payments;
using ShopSphere.Payment.Infrastructure;
using ShopSphere.Payment.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddShopSphereApi(builder.Configuration);
services.AddOpenApi();
services.AddApplicationCore(typeof(PaymentRequestedHandler).Assembly);
services.AddShopSphereDbContext<PaymentDbContext>(builder.Configuration);
services.AddScoped<IPaymentRepository, PaymentRepository>();
services.AddSingleton<IPaymentGateway, FakePaymentGateway>();
services.AddShopSphereEventBus(builder.Configuration, "payment");
services.SubscribeIntegrationEvent<PaymentRequestedIntegrationEvent, PaymentRequestedHandler>();

var app = builder.Build();
await app.EnsureDatabaseAsync<PaymentDbContext>();
app.UseShopSphereApi();
if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.MapGet("/api/payments/order/{orderId:guid}", async (Guid orderId, IPaymentRepository repo, CancellationToken ct) =>
{
    var p = await repo.GetByOrderAsync(orderId, ct);
    return p is null ? Results.NotFound() : Results.Ok(new { p.Id, p.OrderId, p.Amount, p.Currency, Status = p.Status.ToString(), p.FailureReason });
}).RequireAuthorization();

app.Run();

public partial class Program;
