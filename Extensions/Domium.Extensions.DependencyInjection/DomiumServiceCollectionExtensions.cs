using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace Domium.Extensions.DependencyInjection;

public static class DomiumServiceCollectionExtensions
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IServiceCollection AddDomium(
        this IServiceCollection services,
        Action<DomiumOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new DomiumOptions();
        options.AddApplicationAssembly(Assembly.GetCallingAssembly());

        configure?.Invoke(options);

        var builder = new DomiumBuilder(services, options);

        DomiumRegistrar.Register(builder);

        return services;
    }
}
