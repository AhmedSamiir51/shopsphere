using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ShopSphere.BuildingBlocks.Application.Messaging;
using ShopSphere.BuildingBlocks.Domain;
using ShopSphere.Contracts;

namespace ShopSphere.BuildingBlocks.EventBus.Outbox;

/// <summary>
/// The transactional outbox. On every SaveChanges it:
/// 1. dispatches pending domain events to MediatR handlers (which may collect
///    integration events and mutate more aggregates), looping until quiescent;
/// 2. appends one outbox row per collected integration event to the same context.
/// Everything is then committed in ONE database transaction, so state changes and
/// their events can never diverge. A background dispatcher publishes the rows
/// afterwards (at-least-once).
/// </summary>
public sealed class OutboxInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const int MaxDomainEventDispatchRounds = 5;

    private readonly IMediator _mediator;
    private readonly IIntegrationEventCollector _collector;

    public OutboxInterceptor(IMediator mediator, IIntegrationEventCollector collector)
    {
        _mediator = mediator;
        _collector = collector;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await DispatchDomainEventsAsync(eventData.Context, cancellationToken);
            AddOutboxMessages(eventData.Context);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(DbContext context, CancellationToken cancellationToken)
    {
        for (var round = 0; round < MaxDomainEventDispatchRounds; round++)
        {
            var entries = context.ChangeTracker
                .Entries<Entity>()
                .Where(e => e.Entity.DomainEvents.Count > 0)
                .ToList();

            if (entries.Count == 0)
            {
                return;
            }

            var domainEvents = entries.SelectMany(e => e.Entity.DomainEvents).ToList();
            foreach (var entry in entries)
            {
                entry.Entity.ClearDomainEvents();
            }

            foreach (var domainEvent in domainEvents)
            {
                await _mediator.Publish(domainEvent, cancellationToken);
            }
        }
    }

    private void AddOutboxMessages(DbContext context)
    {
        if (_collector.CollectedEvents.Count == 0)
        {
            return;
        }

        foreach (var integrationEvent in _collector.CollectedEvents)
        {
            context.Set<OutboxMessage>().Add(new OutboxMessage
            {
                Id = integrationEvent.Id,
                Type = integrationEvent.GetType().Name,
                Content = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), JsonOptions),
                OccurredOnUtc = integrationEvent.OccurredOnUtc
            });
        }

        _collector.Clear();
    }
}
