using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Ordering.Domain.Orders;
using ShopSphere.Ordering.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace ShopSphere.Ordering.IntegrationTests;

/// <summary>Runs against a real PostgreSQL started by Testcontainers (needs Docker, e.g. CI).</summary>
public sealed class OrderPersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public Task InitializeAsync() => _pg.StartAsync();
    public Task DisposeAsync() => _pg.DisposeAsync().AsTask();

    private OrderingDbContext NewDb() =>
        new(new DbContextOptionsBuilder<OrderingDbContext>().UseNpgsql(_pg.GetConnectionString()).Options);

    [Fact]
    public async Task Order_round_trips_with_items_and_status()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        Guid orderId;

        await using (var db = NewDb())
        {
            await db.Database.EnsureCreatedAsync();
            var order = Order.Place(userId, "EGP", new[] { (productId, "Headphones", 2, 1500m) });
            await new OrderRepository(db).AddAsync(order, CancellationToken.None);
            await db.SaveChangesAsync();
            orderId = order.Id;
        }

        await using (var db = NewDb())
        {
            var loaded = await new OrderRepository(db).GetByIdAsync(orderId, CancellationToken.None);
            loaded.Should().NotBeNull();
            loaded!.Items.Should().ContainSingle().Which.Quantity.Should().Be(2);
            loaded.Total.Amount.Should().Be(3000m);
            loaded.MarkStockReserved();
            loaded.Confirm();
            await db.SaveChangesAsync();
        }

        await using (var db = NewDb())
        {
            var orders = await new OrderRepository(db).GetByUserAsync(userId, CancellationToken.None);
            orders.Should().ContainSingle().Which.Status.Should().Be(OrderStatus.Confirmed);
        }
    }
}
