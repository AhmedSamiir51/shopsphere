using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using ShopSphere.BuildingBlocks.Application.Messaging;
using ShopSphere.Contracts;

namespace ShopSphere.BuildingBlocks.EventBus.RabbitMq;

/// <summary>
/// RabbitMQ implementation of <see cref="IEventBus"/>. Publishes to the
/// "shopsphere.events" topic exchange; the routing key is the event type name
/// (the convention subscribers bind on).
/// </summary>
public sealed class RabbitMqEventBus : IEventBus, IAsyncDisposable
{
    public const string ExchangeName = "shopsphere.events";
    public const string DeadLetterExchangeName = "shopsphere.dlx";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqEventBus> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;

    public RabbitMqEventBus(RabbitMqOptions options, ILogger<RabbitMqEventBus> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        var eventType = integrationEvent.GetType();

        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);

        var body = JsonSerializer.SerializeToUtf8Bytes(integrationEvent, eventType, JsonOptions);
        var properties = new BasicProperties
        {
            Persistent = true,
            MessageId = integrationEvent.Id.ToString(),
            Type = eventType.Name,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        await channel.BasicPublishAsync(ExchangeName, eventType.Name, mandatory: false, basicProperties: properties,
            body: body, cancellationToken: cancellationToken);

        _logger.LogInformation("Published {EventType} ({EventId}) to {Exchange}",
            eventType.Name, integrationEvent.Id, ExchangeName);
    }

    private async Task<IConnection> GetConnectionAsync(CancellationToken ct)
    {
        if (_connection is { IsOpen: true }) return _connection;

        await _gate.WaitAsync(ct);
        try
        {
            if (_connection is not { IsOpen: true })
            {
                _connection = await RabbitMqConnectionFactory.ConnectAsync(_options, _logger, ct);
            }
            return _connection;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null) await _connection.DisposeAsync();
        _gate.Dispose();
    }
}
