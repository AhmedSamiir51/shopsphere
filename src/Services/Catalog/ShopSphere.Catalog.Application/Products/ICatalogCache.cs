namespace ShopSphere.Catalog.Application.Products;

/// <summary>Cache-aside store for read models. Implementations must never throw: a cache outage degrades to a miss.
/// Invalidation bumps a namespace version, so every cached list becomes unreachable at once (O(1), no key scans).</summary>
public interface ICatalogCache
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct) where T : class;
    Task InvalidateAsync(CancellationToken ct);
}

public sealed class NoCatalogCache : ICatalogCache
{
    public Task<T?> GetAsync<T>(string key, CancellationToken ct) where T : class => Task.FromResult<T?>(null);
    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct) where T : class => Task.CompletedTask;
    public Task InvalidateAsync(CancellationToken ct) => Task.CompletedTask;
}
