using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ShopSphere.BuildingBlocks.EventBus.Inbox;
using ShopSphere.Contracts;

namespace ShopSphere.BuildingBlocks.EventBus.RabbitMq;

/// <summary>
/// Background service that consumes integration events from this service's queue.
/// Pipeline per message: deserialize by routing-key convention -> inbox idempotency
/// check -> invoke the scoped handler -> mark processed -> ack. Failures are nacked
/// without requeue so they land in the dead-letter exchange instead of looping.
/// </summary>
public sealed class RabbitMqSubscriber : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RabbitMqOptions _options;
    private readonly EventBusSubscriptionRegistry _registry;
    private readonly IServiceProvider _serviceProvider;
    private readonly IInboxStore _inbox;
    private readonly ILogger<RabbitMqSubscriber> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqSubscriber(
        RabbitMqOptions options,
        EventBusSubscriptionRegistry registry,
        IServiceProvider serviceProvider,
        IInboxStore inbox,
        ILogger<RabbitMqSubscriber> logger)
    {
        _options = options;
        _registry = registry;
        _serviceProvider = serviceProvider;
        _inbox = inbox;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_registry.Subscriptions.Count == 0)
        {
            _logger.LogInformation("No integration event subscriptions registered, subscriber idle");
            return;
        }

        _connection = await RabbitMqConnectionFactory.ConnectAsync(_options, _logger, stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.ExchangeDeclareAsync(RabbitMqEventBus.ExchangeName, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
        await _channel.ExchangeDeclareAsync(RabbitMqEventBus.DeadLetterExchangeName, ExchangeType.Fanout, durable: true, cancellationToken: stoppingToken);

        var queueName = $"shopsphere.{_options.ServiceName}";
        var queueArgs = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = RabbitMqEventBus.DeadLetterExchangeName
        };

        await _channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs, cancellationToken: stoppingToken);
        await _channel.QueueDeclareAsync($"{queueName}.dead", durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await _channel.QueueBindAsync($"{queueName}.dead", RabbitMqEventBus.DeadLetterExchangeName, routingKey: string.Empty, cancellationToken: stoppingToken);

        foreach (var subscription in _registry.Subscriptions)
        {
            await _channel.QueueBindAsync(queueName, RabbitMqEventBus.ExchangeName, subscription.RoutingKey, cancellationToken: stoppingToken);
            _logger.LogInformation("Bound {Queue} to {RoutingKey}", queueName, subscription.RoutingKey);
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += (_, args) => OnMessageAsync(args, stoppingToken);
        await _channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // graceful shutdown
        }
    }

    private async Task OnMessageAsync(BasicDeliverEventArgs args, CancellationToken cancellationToken)
    {
        var subscription = _registry.Find(args.RoutingKey);
        if (subscription is null)
        {
            _logger.LogWarning("No subscription for routing key {RoutingKey}, dead-lettering", args.RoutingKey);
            await _channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false, cancellationToken: cancellationToken);
            return;
        }

        IntegrationEvent? integrationEvent = null;
        var consumerName = subscription.EventType.Name + ".handler";

        try
        {
            integrationEvent = (IntegrationEvent?)JsonSerializer.Deserialize(
                args.Body.Span, subscription.EventType, JsonOptions);

            if (integrationEvent is null)
            {
                throw new InvalidOperationException($"Empty payload for {args.RoutingKey}");
            }

            if (await _inbox.HasProcessedAsync(integrationEvent.Id, consumerName, cancellationToken))
            {
                _logger.LogInformation("Duplicate {EventType} ({EventId}) skipped by inbox",
                    subscription.EventType.Name, integrationEvent.Id);
                await _channel!.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            await subscription.Invoker(scope.ServiceProvider, integrationEvent, cancellationToken);

            await _inbox.MarkProcessedAsync(integrationEvent.Id, consumerName, cancellationToken);
            await _channel!.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: cancellationToken);

            _logger.LogInformation("Consumed {EventType} ({EventId})",
                subscription.EventType.Name, integrationEvent.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process {EventType} ({RoutingKey}), dead-lettering",
                subscription.EventType.Name, args.RoutingKey);
            await _channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false, cancellationToken: cancellationToken);
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
