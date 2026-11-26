using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ShopSphere.BuildingBlocks.Application.Behaviors;

namespace ShopSphere.BuildingBlocks.Application;

/// <summary>
/// Wires up the application layer of a service: MediatR handlers from the given
/// assembly, FluentValidation validators, and the cross-cutting pipeline behaviors.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationCore(
        this IServiceCollection services,
        Assembly handlersAssembly)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(handlersAssembly));
        services.AddValidatorsFromAssembly(handlersAssembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
