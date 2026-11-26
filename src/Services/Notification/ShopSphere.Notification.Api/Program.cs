using System.Reflection;
using MediatR;
using ShopSphere.BuildingBlocks.Application;
using ShopSphere.BuildingBlocks.EventBus;
using ShopSphere.BuildingBlocks.Infrastructure;
using System.Security.Claims;
using ShopSphere.Contracts.Events;
using ShopSphere.Notification.Application.Handlers;
using ShopSphere.Notification.Domain.Notifications;
using ShopSphere.Notification.Infrastructure;
using ShopSphere.Notification.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddShopSphereApi(builder.Configuration);
services.AddOpenApi();
services.AddApplicationCore(typeof(OrderConfirmedNotifier).Assembly);
services.AddShopSphereDbContext<NotificationDbContext>(builder.Configuration);
services.AddScoped<INotificationRepository, NotificationRepository>();
services.AddSingleton<IEmailSender, LoggingEmailSender>();
services.AddShopSphereEventBus(builder.Configuration, "notification");
services.SubscribeIntegrationEvent<UserRegisteredIntegrationEvent, UserRegisteredNotifier>();
services.SubscribeIntegrationEvent<OrderConfirmedIntegrationEvent, OrderConfirmedNotifier>();
services.SubscribeIntegrationEvent<OrderCancelledIntegrationEvent, OrderCancelledNotifier>();

var app = builder.Build();
await app.EnsureDatabaseAsync<NotificationDbContext>();
app.UseShopSphereApi();
if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.MapGet("/api/notifications", async (ClaimsPrincipal u, INotificationRepository repo, CancellationToken ct) =>
    Results.Ok((await repo.GetForUserAsync(u.GetUserId(), ct))
        .Select(n => new { n.Id, n.Subject, n.Body, n.CreatedOnUtc, n.ReadOnUtc }))).RequireAuthorization();

app.Run();

public partial class Program;
