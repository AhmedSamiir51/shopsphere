using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ShopSphere.BuildingBlocks.EventBus.RabbitMq;
using ShopSphere.Contracts.Events;
using Testcontainers.RabbitMq;

namespace ShopSphere.Ordering.IntegrationTests;

/// <summary>Publishes through the real RabbitMqEventBus to a real broker (Testcontainers) and consumes it with a raw client.</summary>
public sealed class RabbitMqPublishTests : IAsyncLifetime
{
    private readonly RabbitMqContainer _mq = new RabbitMqBuilder("rabbitmq:4-alpine").Build();

    public Task InitializeAsync() => _mq.StartAsync();
    public Task DisposeAsync() => _mq.DisposeAsync().AsTask();

    [Fact]
    public async Task Published_event_is_routed_by_type_name_with_persistent_json_body()
    {
        var uri = _mq.GetConnectionString();
        var factory = new ConnectionFactory { Uri = new Uri(uri) };
        await using var conn = await factory.CreateConnectionAsync();
        await using var ch = await conn.CreateChannelAsync();

        await ch.ExchangeDeclareAsync(RabbitMqEventBus.ExchangeName, ExchangeType.Topic, durable: true);
        var q = await ch.QueueDeclareAsync(queue: "test.q", durable: false, exclusive: false, autoDelete: true);
        await ch.QueueBindAsync(q.QueueName, RabbitMqEventBus.ExchangeName, nameof(OrderConfirmedIntegrationEvent));

        var received = new TaskCompletionSource<BasicDeliverEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        var consumer = new AsyncEventingBasicConsumer(ch);
        consumer.ReceivedAsync += (_, ea) => { received.TrySetResult(ea); return Task.CompletedTask; };
        await ch.BasicConsumeAsync(q.QueueName, autoAck: true, consumer);

        await using var bus = new RabbitMqEventBus(
            new RabbitMqOptions { ConnectionUri = uri, ServiceName = "tests" }, NullLogger<RabbitMqEventBus>.Instance);
        var evt = new OrderConfirmedIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), 120.5m, "EGP");
        await bus.PublishAsync(evt);

        var msg = await received.Task.WaitAsync(TimeSpan.FromSeconds(20));
        msg.RoutingKey.Should().Be(nameof(OrderConfirmedIntegrationEvent));
        msg.BasicProperties.MessageId.Should().Be(evt.Id.ToString());
        msg.BasicProperties.DeliveryMode.Should().Be(DeliveryModes.Persistent);
        var body = JsonSerializer.Deserialize<OrderConfirmedIntegrationEvent>(msg.Body.Span, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        body!.OrderId.Should().Be(evt.OrderId);
        body.TotalAmount.Should().Be(120.5m);
    }
}
