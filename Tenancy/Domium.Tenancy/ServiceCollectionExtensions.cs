using Domium.Tenancy.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Domium.Tenancy;

/// <summary>
/// Dependency injection helpers for Domium tenancy.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the default ambient tenant context implementation.
    /// </summary>
    public static IServiceCollection AddDomiumTenancy(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.TryAddSingleton<IDomiumTenantContextAccessor, AsyncLocalDomiumTenantContextAccessor>();
        services.TryAddSingleton<IDomiumTenantAccessor>(
            provider => provider.GetRequiredService<IDomiumTenantContextAccessor>());
        services.TryAddSingleton<IDomiumTenantScopeFactory, DomiumTenantScopeFactory>();
        services.TryAddScoped<IDomiumTenantNameResolver, AmbientDomiumTenantNameResolver>();
        services.TryAddSingleton<IDomiumTenantConnectionStringResolver, DomiumTenantConnectionStringResolver>();

        return services;
    }
}
