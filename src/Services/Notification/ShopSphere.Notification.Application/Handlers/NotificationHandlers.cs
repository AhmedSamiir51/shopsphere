using ShopSphere.BuildingBlocks.Application.Abstractions;
using ShopSphere.BuildingBlocks.Application.Messaging;
using ShopSphere.Contracts.Events;
using ShopSphere.Notification.Domain.Notifications;

namespace ShopSphere.Notification.Application.Handlers;

/// <summary>Turns business events into user-facing notifications. Consumer only, never publishes.</summary>
public abstract class NotifyBase
{
    private readonly INotificationRepository _repo;
    private readonly IEmailSender _email;
    private readonly IUnitOfWork _uow;

    protected NotifyBase(INotificationRepository repo, IEmailSender email, IUnitOfWork uow)
    {
        _repo = repo; _email = email; _uow = uow;
    }

    protected async Task NotifyAsync(Guid userId, string subject, string body, CancellationToken ct)
    {
        await _repo.AddAsync(Domain.Notifications.Notification.Create(userId, NotificationChannel.InApp, subject, body), ct);
        await _email.SendAsync(userId, subject, body, ct);
        await _uow.SaveChangesAsync(ct);
    }
}

public sealed class UserRegisteredNotifier : NotifyBase, IIntegrationEventHandler<UserRegisteredIntegrationEvent>
{
    public UserRegisteredNotifier(INotificationRepository r, IEmailSender e, IUnitOfWork u) : base(r, e, u) { }
    public Task Handle(UserRegisteredIntegrationEvent e, CancellationToken ct) =>
        NotifyAsync(e.UserId, "Welcome to ShopSphere", $"Hi {e.UserName}, your account is ready.", ct);
}

public sealed class OrderConfirmedNotifier : NotifyBase, IIntegrationEventHandler<OrderConfirmedIntegrationEvent>
{
    public OrderConfirmedNotifier(INotificationRepository r, IEmailSender e, IUnitOfWork u) : base(r, e, u) { }
    public Task Handle(OrderConfirmedIntegrationEvent e, CancellationToken ct) =>
        NotifyAsync(e.UserId, "Order confirmed", $"Order {e.OrderId} is confirmed. Total {e.TotalAmount:0.00} {e.Currency}.", ct);
}

public sealed class OrderCancelledNotifier : NotifyBase, IIntegrationEventHandler<OrderCancelledIntegrationEvent>
{
    public OrderCancelledNotifier(INotificationRepository r, IEmailSender e, IUnitOfWork u) : base(r, e, u) { }
    public Task Handle(OrderCancelledIntegrationEvent e, CancellationToken ct) =>
        NotifyAsync(e.UserId, "Order cancelled", $"Order {e.OrderId} was cancelled: {e.Reason}.", ct);
}
