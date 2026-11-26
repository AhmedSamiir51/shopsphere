using ShopSphere.BuildingBlocks.Application.Messaging;
using ShopSphere.Contracts;

namespace ShopSphere.BuildingBlocks.EventBus.Outbox;

/// <summary>Scoped collector: application/domain-event handlers hand integration events
/// to it; the outbox interceptor persists them in the same transaction.</summary>
public sealed class IntegrationEventCollector : IIntegrationEventCollector
{
    private readonly List<IntegrationEvent> _events = new();

    public IReadOnlyCollection<IntegrationEvent> CollectedEvents => _events;

    public void Collect(IntegrationEvent integrationEvent) => _events.Add(integrationEvent);

    public void Clear() => _events.Clear();
}
