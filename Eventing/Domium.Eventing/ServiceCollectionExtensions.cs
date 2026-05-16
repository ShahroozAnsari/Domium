using Domium.Eventing.Abstractions.External;
using Domium.Eventing.Abstractions.Internal;
using Domium.Eventing.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Domium.Eventing;

/// <summary>
/// Dependency injection helpers for provider-neutral Domium eventing.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Domium's in-process event publisher and a no-op external publisher.
    /// External transport packages can replace the no-op publisher.
    /// </summary>
    public static IServiceCollection AddDomiumEventing(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.TryAddScoped<IInternalEventPublisher, InternalEventPublisher>();
        services.TryAddScoped<IExternalEventPublisher, NullExternalEventPublisher>();

        return services;
    }
}
