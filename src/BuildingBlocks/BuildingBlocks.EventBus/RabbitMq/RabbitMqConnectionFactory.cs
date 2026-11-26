using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace ShopSphere.BuildingBlocks.EventBus.RabbitMq;

/// <summary>Opens the broker connection with retries so services survive the broker
/// container still booting (docker-compose race). RabbitMQ.Client 7 is fully async.</summary>
internal static class RabbitMqConnectionFactory
{
    public static async Task<IConnection> ConnectAsync(RabbitMqOptions options, ILogger logger, CancellationToken ct = default)
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri(options.ConnectionUri),
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            ClientProvidedName = $"shopsphere-{options.ServiceName}"
        };

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var connection = await factory.CreateConnectionAsync(ct);
                logger.LogInformation("Connected to RabbitMQ at {Host}", factory.Uri.Host);
                return connection;
            }
            catch (Exception ex) when (attempt < options.ConnectRetryCount && !ct.IsCancellationRequested)
            {
                logger.LogWarning(ex, "RabbitMQ not ready (attempt {Attempt}/{Max}), retrying in {Delay}s",
                    attempt, options.ConnectRetryCount, options.ConnectRetryDelay.TotalSeconds);
                await Task.Delay(options.ConnectRetryDelay, ct);
            }
        }
    }
}
