using System.Text.Json;
using Microsoft.Extensions.Logging;
using ShopSphere.Catalog.Application.Products;
using StackExchange.Redis;

namespace ShopSphere.Catalog.Infrastructure;

/// <summary>Redis cache-aside with versioned keys: "catalog:v{n}:{key}". Bumping {n} invalidates everything.</summary>
public sealed class RedisCatalogCache(IConnectionMultiplexer redis, ILogger<RedisCatalogCache> log) : ICatalogCache
{
    private const string VersionKey = "catalog:version";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private async Task<string> KeyAsync(string key)
    {
        var v = await redis.GetDatabase().StringGetAsync(VersionKey);
        return $"catalog:v{(v.HasValue ? v.ToString() : "0")}:{key}";
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct) where T : class
    {
        try
        {
            var raw = await redis.GetDatabase().StringGetAsync(await KeyAsync(key));
            return raw.HasValue ? JsonSerializer.Deserialize<T>((string)raw!, Json) : null;
        }
        catch (Exception ex) when (ex is RedisException or JsonException)
        {
            log.LogWarning(ex, "Cache read failed, treating as miss");
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct) where T : class
    {
        try { await redis.GetDatabase().StringSetAsync(await KeyAsync(key), JsonSerializer.Serialize(value, Json), ttl); }
        catch (RedisException ex) { log.LogWarning(ex, "Cache write failed"); }
    }

    public async Task InvalidateAsync(CancellationToken ct)
    {
        try { await redis.GetDatabase().StringIncrementAsync(VersionKey); }
        catch (RedisException ex) { log.LogWarning(ex, "Cache invalidation failed"); }
    }
}
