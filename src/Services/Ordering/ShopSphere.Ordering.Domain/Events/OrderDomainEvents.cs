using ShopSphere.BuildingBlocks.Domain;

namespace ShopSphere.Ordering.Domain.Events;

public sealed record OrderPlacedDomainEvent(Guid OrderId) : DomainEvent;
public sealed record OrderConfirmedDomainEvent(Guid OrderId) : DomainEvent;
public sealed record OrderCancelledDomainEvent(Guid OrderId, string Reason) : DomainEvent;
