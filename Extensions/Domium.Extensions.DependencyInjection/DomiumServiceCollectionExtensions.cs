using System;
using Microsoft.Extensions.DependencyInjection;

namespace Domium.Extensions.DependencyInjection;

public static class DomiumServiceCollectionExtensions
{
    public static IServiceCollection AddDomium(
        this IServiceCollection services,
        Action<DomiumOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new DomiumOptions();

        configure?.Invoke(options);

        var builder = new DomiumBuilder(services, options);

        DomiumRegistrar.Register(builder);

        return services;
    }
}