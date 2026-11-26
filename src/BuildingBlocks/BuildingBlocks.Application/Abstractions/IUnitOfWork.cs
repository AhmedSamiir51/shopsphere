namespace ShopSphere.BuildingBlocks.Application.Abstractions;

/// <summary>Commits the current unit of work: aggregate state + outbox messages atomically.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
