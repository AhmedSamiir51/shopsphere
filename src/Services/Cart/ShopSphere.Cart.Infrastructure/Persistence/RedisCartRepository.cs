using System.Text.Json;
using ShopSphere.Cart.Domain.Carts;
using StackExchange.Redis;

namespace ShopSphere.Cart.Infrastructure.Persistence;

public sealed class RedisCartRepository : ICartRepository
{
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(14);
    private readonly IConnectionMultiplexer _redis;
    public RedisCartRepository(IConnectionMultiplexer redis) => _redis = redis;

    private static string Key(Guid userId) => $"cart:{userId}";

    public async Task<ShoppingCart> GetAsync(Guid userId, CancellationToken ct)
    {
        var json = await _redis.GetDatabase().StringGetAsync(Key(userId));
        if (json.IsNullOrEmpty) return new ShoppingCart(userId);
        var lines = JsonSerializer.Deserialize<List<CartLine>>(json.ToString()) ?? new();
        return new ShoppingCart(userId, lines);
    }

    public Task SaveAsync(ShoppingCart cart, CancellationToken ct) =>
        _redis.GetDatabase().StringSetAsync(Key(cart.UserId), JsonSerializer.Serialize(cart.Lines), Ttl);

    public Task DeleteAsync(Guid userId, CancellationToken ct) => _redis.GetDatabase().KeyDeleteAsync(Key(userId));
}
