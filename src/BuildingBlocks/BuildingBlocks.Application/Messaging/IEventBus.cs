using ShopSphere.Contracts;

namespace ShopSphere.BuildingBlocks.Application.Messaging;

/// <summary>
/// Publishes integration events to the message broker. Application code should
/// normally prefer <see cref="IIntegrationEventCollector"/> (transactional outbox);
/// this abstraction is for infrastructure (the outbox dispatcher) and tests.
/// </summary>
public interface IEventBus
{
    Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Collects integration events raised while handling a use case. Collected events
/// are appended to the outbox inside the same database transaction as the state
/// change (see the outbox interceptor), guaranteeing at-least-once publication
/// without a distributed transaction.
/// </summary>
public interface IIntegrationEventCollector
{
    void Collect(IntegrationEvent integrationEvent);
    IReadOnlyCollection<IntegrationEvent> CollectedEvents { get; }
    void Clear();
}
