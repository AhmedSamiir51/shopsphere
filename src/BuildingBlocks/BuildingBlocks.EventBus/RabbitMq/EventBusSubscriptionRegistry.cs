using ShopSphere.Contracts;

namespace ShopSphere.BuildingBlocks.EventBus.RabbitMq;

/// <summary>A single subscription: routing key + event type + how to invoke its handler.</summary>
public sealed class EventBusSubscription
{
    public required string RoutingKey { get; init; }
    public required Type EventType { get; init; }
    public required Func<IServiceProvider, IntegrationEvent, CancellationToken, Task> Invoker { get; init; }
}

/// <summary>Registry of all integration-event subscriptions of a service.</summary>
public sealed class EventBusSubscriptionRegistry
{
    private readonly List<EventBusSubscription> _subscriptions = new();

    public IReadOnlyList<EventBusSubscription> Subscriptions => _subscriptions;

    public void Add(EventBusSubscription subscription) => _subscriptions.Add(subscription);

    public EventBusSubscription? Find(string routingKey) =>
        _subscriptions.FirstOrDefault(s => s.RoutingKey == routingKey);
}
