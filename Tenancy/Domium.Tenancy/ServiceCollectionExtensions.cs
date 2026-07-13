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
    /// Registers ambient tenant resolution and tenant database connection resolution.
    /// Applications typically override <see cref="IDomiumTenantResolver"/> with a
    /// request-specific implementation (e.g. reading a claim/header).
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
        services.TryAddScoped<IDomiumTenantResolver, AmbientDomiumTenantResolver>();
        services.TryAddScoped<IDomiumTenantConnectionResolver, DomiumTenantConnectionResolver>();

        return services;
    }
}
