using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShopSphere.BuildingBlocks.Application.Messaging;
using ShopSphere.Contracts;

namespace ShopSphere.BuildingBlocks.EventBus.Outbox;

/// <summary>
/// Polls the outbox table and publishes pending messages to the broker, marking them
/// processed. Per-message failures are recorded on the row and retried on the next
/// pass, so one poisoned message never blocks the queue.
/// </summary>
public sealed class OutboxDispatcher<TContext> : BackgroundService
    where TContext : DbContext, IOutboxDbContext
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxDispatcher<TContext>> _logger;

    public OutboxDispatcher(IServiceProvider serviceProvider, ILogger<OutboxDispatcher<TContext>> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Outbox dispatch pass failed");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task DispatchPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        var pending = await context.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        foreach (var message in pending)
        {
            try
            {
                var eventType = ContractsEventTypes.Resolve(message.Type);
                var integrationEvent = (IntegrationEvent?)JsonSerializer.Deserialize(
                    message.Content, eventType, new JsonSerializerOptions(JsonSerializerDefaults.Web));

                if (integrationEvent is null)
                {
                    throw new InvalidOperationException($"Could not deserialize outbox message {message.Id}");
                }

                await eventBus.PublishAsync(integrationEvent, cancellationToken);
                message.ProcessedOnUtc = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox message {MessageId} ({Type})", message.Id, message.Type);
                message.Error = ex.Message;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Resolves integration event types stored by name in the outbox.</summary>
internal static class ContractsEventTypes
{
    private static readonly Lazy<Dictionary<string, Type>> Types = new(() =>
        typeof(IntegrationEvent).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(IntegrationEvent).IsAssignableFrom(t))
            .ToDictionary(t => t.Name));

    public static Type Resolve(string name) =>
        Types.Value.TryGetValue(name, out var type)
            ? type
            : throw new InvalidOperationException($"Unknown integration event type '{name}'");
}
