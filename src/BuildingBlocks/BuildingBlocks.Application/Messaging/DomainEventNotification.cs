using MediatR;
using ShopSphere.BuildingBlocks.Domain;

namespace ShopSphere.BuildingBlocks.Application.Messaging;

/// <summary>Adapter that lets in-process handlers react to a domain event through MediatR
/// without the Domain layer depending on MediatR. Handle <c>INotificationHandler&lt;DomainEventNotification&gt;</c>
/// and pattern-match on <see cref="Event"/>.</summary>
public sealed record DomainEventNotification(IDomainEvent Event) : INotification;
