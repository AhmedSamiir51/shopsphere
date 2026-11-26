namespace ShopSphere.Notification.Domain.Notifications;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken ct);
    Task<IReadOnlyList<Notification>> GetForUserAsync(Guid userId, CancellationToken ct);
}

/// <summary>Port to an email provider. The default adapter logs the email (no SMTP needed for the demo).</summary>
public interface IEmailSender
{
    Task SendAsync(Guid userId, string subject, string body, CancellationToken ct);
}
