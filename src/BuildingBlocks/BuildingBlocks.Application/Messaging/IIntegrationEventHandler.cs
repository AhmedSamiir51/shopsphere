using ShopSphere.Contracts;

namespace ShopSphere.BuildingBlocks.Application.Messaging;

/// <summary>
/// Handles an integration event published by another service. Implementations live
/// in the consuming service's application layer and are invoked by the event bus
/// subscriber through the inbox (idempotent consumer).
/// </summary>
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IntegrationEvent
{
    Task Handle(TEvent integrationEvent, CancellationToken cancellationToken);
}
