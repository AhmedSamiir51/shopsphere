using ShopSphere.BuildingBlocks.Domain;

namespace ShopSphere.Notification.Domain.Notifications;

public enum NotificationChannel { Email = 0, InApp = 1 }

public sealed class Notification : AggregateRoot
{
    public Guid UserId { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? ReadOnUtc { get; private set; }

    private Notification() { }

    public static Notification Create(Guid userId, NotificationChannel channel, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(subject)) throw new DomainException("Subject is required.");
        return new Notification { UserId = userId, Channel = channel, Subject = subject, Body = body, CreatedOnUtc = DateTime.UtcNow };
    }

    public void MarkRead() => ReadOnUtc ??= DateTime.UtcNow;
}
