using Microsoft.EntityFrameworkCore;
using ShopSphere.Notification.Domain.Notifications;

namespace ShopSphere.Notification.Infrastructure.Persistence;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _db;
    public NotificationRepository(NotificationDbContext db) => _db = db;

    public async Task AddAsync(Domain.Notifications.Notification n, CancellationToken ct) => await _db.Notifications.AddAsync(n, ct);

    public async Task<IReadOnlyList<Domain.Notifications.Notification>> GetForUserAsync(Guid userId, CancellationToken ct) =>
        await _db.Notifications.AsNoTracking().Where(n => n.UserId == userId).OrderByDescending(n => n.CreatedOnUtc).Take(50).ToListAsync(ct);
}
