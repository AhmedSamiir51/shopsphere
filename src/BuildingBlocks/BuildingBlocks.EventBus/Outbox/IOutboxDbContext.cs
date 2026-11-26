using Microsoft.EntityFrameworkCore;

namespace ShopSphere.BuildingBlocks.EventBus.Outbox;

/// <summary>Implemented by every service DbContext so the outbox dispatcher can
/// reach its outbox table.</summary>
public interface IOutboxDbContext
{
    DbSet<OutboxMessage> OutboxMessages { get; }
}
