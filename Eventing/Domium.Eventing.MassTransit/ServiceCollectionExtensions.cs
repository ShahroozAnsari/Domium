using Domium.Eventing.Abstractions.External;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Domium.Eventing.MassTransit;

/// <summary>
/// Dependency injection helpers for Domium's MassTransit external event adapter.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Replaces the default external event publisher with the MassTransit publisher.
    /// </summary>
    public static IServiceCollection AddDomiumMassTransitEventing(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddScoped<IExternalEventPublisher, MassTransitExternalEventPublisher>();

        return services;
    }

    /// <summary>
    /// Registers a Domium external event consumer adapter for a specific event type.
    /// </summary>
    public static IBusRegistrationConfigurator AddDomiumExternalEventConsumer<TExternalEvent>(
        this IBusRegistrationConfigurator configurator)
        where TExternalEvent : class, IExternalEvent
    {
        if (configurator == null)
        {
            throw new ArgumentNullException(nameof(configurator));
        }

        configurator.AddConsumer<MassTransitExternalEventConsumer<TExternalEvent>>();
        return configurator;
    }
}
