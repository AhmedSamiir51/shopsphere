using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ShopSphere.BuildingBlocks.Application.Abstractions;
using ShopSphere.BuildingBlocks.Application.Messaging;
using ShopSphere.BuildingBlocks.EventBus.Inbox;
using ShopSphere.BuildingBlocks.EventBus.Outbox;
using ShopSphere.BuildingBlocks.EventBus.RabbitMq;
using ShopSphere.Contracts;

namespace ShopSphere.BuildingBlocks.EventBus;

/// <summary>Composition helpers shared by every service: EF Core + outbox, RabbitMQ
/// event bus + inbox, and subscription registration.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers <typeparamref name="TContext"/> against PostgreSQL with the outbox
    /// interceptor, plus the unit of work and the outbox dispatcher hosted service.
    /// </summary>
    public static IServiceCollection AddShopSphereDbContext<TContext>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TContext : DbContext, IOutboxDbContext, IUnitOfWork
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is missing");

        services.AddScoped<IIntegrationEventCollector, IntegrationEventCollector>();
        services.AddScoped<OutboxInterceptor>();

        services.AddDbContext<TContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString);
            options.AddInterceptors(sp.GetRequiredService<OutboxInterceptor>());
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TContext>());
        services.AddHostedService<OutboxDispatcher<TContext>>();

        return services;
    }

    /// <summary>
    /// Registers the RabbitMQ event bus (publisher), the subscription registry, the
    /// subscriber hosted service and the inbox store.
    /// </summary>
    public static IServiceCollection AddShopSphereEventBus(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        var options = new RabbitMqOptions { ServiceName = serviceName };
        configuration.GetSection("RabbitMq").Bind(options);
        var rabbitConnection = configuration.GetConnectionString("RabbitMQ");
        if (!string.IsNullOrWhiteSpace(rabbitConnection))
        {
            options.ConnectionUri = rabbitConnection;
        }
        options.ServiceName = serviceName;

        services.AddSingleton(options);
        services.AddSingleton<IEventBus, RabbitMqEventBus>();
        services.AddSingleton(sp =>
        {
            var registry = new EventBusSubscriptionRegistry();
            foreach (var startup in sp.GetServices<IStartupSubscription>())
            {
                registry.Add(startup.ToSubscription());
            }
            return registry;
        });

        var dbConnectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is missing");
        services.AddSingleton<IInboxStore>(new NpgsqlInboxStore(dbConnectionString));

        services.AddHostedService(sp => new RabbitMqSubscriber(
            sp.GetRequiredService<RabbitMqOptions>(),
            sp.GetRequiredService<EventBusSubscriptionRegistry>(),
            sp,
            sp.GetRequiredService<IInboxStore>(),
            sp.GetRequiredService<ILogger<RabbitMqSubscriber>>()));

        return services;
    }

    /// <summary>
    /// Subscribes <typeparamref name="THandler"/> to <typeparamref name="TEvent"/>:
    /// registers the handler in DI and adds the routing-key binding to the registry.
    /// The routing key convention is the event type name.
    /// </summary>
    public static IServiceCollection SubscribeIntegrationEvent<TEvent, THandler>(
        this IServiceCollection services)
        where TEvent : IntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        services.AddScoped<IIntegrationEventHandler<TEvent>, THandler>();
        services.AddSingleton<IStartupSubscription>(new StartupSubscription(
            typeof(TEvent).Name,
            typeof(TEvent),
            (sp, evt, ct) => sp.GetRequiredService<IIntegrationEventHandler<TEvent>>().Handle((TEvent)evt, ct)));

        return services;
    }

    /// <summary>Marker carrying one subscription until the registry is populated.</summary>
    public interface IStartupSubscription
    {
        EventBusSubscription ToSubscription();
    }

    private sealed record StartupSubscription(
        string RoutingKey,
        Type EventType,
        Func<IServiceProvider, IntegrationEvent, CancellationToken, Task> Invoker) : IStartupSubscription
    {
        public EventBusSubscription ToSubscription() => new()
        {
            RoutingKey = RoutingKey,
            EventType = EventType,
            Invoker = Invoker
        };
    }
}
