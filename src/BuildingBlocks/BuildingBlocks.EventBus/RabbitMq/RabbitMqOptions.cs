namespace ShopSphere.BuildingBlocks.EventBus.RabbitMq;

/// <summary>Settings for the RabbitMQ connection. Bound from the "RabbitMq" configuration
/// section; the connection string ("ConnectionStrings:RabbitMQ", amqp:// URI) wins when present.</summary>
public sealed class RabbitMqOptions
{
    /// <summary>amqp:// connection URI, e.g. amqp://guest:guest@rabbitmq:5672/</summary>
    public string ConnectionUri { get; set; } = "amqp://guest:guest@localhost:5672/";

    /// <summary>Name of the owning service; becomes the queue name suffix.</summary>
    public string ServiceName { get; set; } = "unknown";

    /// <summary>How many times to retry the initial broker connection (containers boot race).</summary>
    public int ConnectRetryCount { get; set; } = 10;

    /// <summary>Delay between broker connection retries.</summary>
    public TimeSpan ConnectRetryDelay { get; set; } = TimeSpan.FromSeconds(3);
}
