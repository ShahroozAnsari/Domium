using Domium.Tenancy.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Domium.Persistence.EntityFrameworkCore;

/// <summary>
/// Registers tenant-scoped DbContexts whose connection points at the current tenant's
/// database — resolved per request from the ambient tenant and the {tenant}_{service}
/// naming convention. The provider (e.g. Npgsql) is supplied by the caller so the framework
/// stays provider-agnostic.
/// </summary>
public static class TenantDbContextServiceCollectionExtensions
{
    /// <summary>
    /// Registers a Domium write DbContext (repositories, unit of work, interceptors) bound
    /// to the current tenant's database.
    /// </summary>
    public static IServiceCollection AddDomiumTenantDbContext<TContext>(
        this IServiceCollection services,
        string serviceName,
        string baseConnectionString,
        Action<DbContextOptionsBuilder, string> configureProvider)
        where TContext : DbContext =>
        services.AddDomiumEntityFrameworkCore<TContext>((provider, options) =>
            configureProvider(options, ResolveConnection(provider, serviceName, baseConnectionString)));

    /// <summary>
    /// Registers a plain DbContext (e.g. a read model) bound to the current tenant's database.
    /// </summary>
    public static IServiceCollection AddTenantDbContext<TContext>(
        this IServiceCollection services,
        string serviceName,
        string baseConnectionString,
        Action<DbContextOptionsBuilder, string> configureProvider)
        where TContext : DbContext
    {
        services.AddDbContext<TContext>((provider, options) =>
            configureProvider(options, ResolveConnection(provider, serviceName, baseConnectionString)));

        return services;
    }

    private static string ResolveConnection(IServiceProvider provider, string serviceName, string baseConnectionString) =>
        provider.GetRequiredService<IDomiumTenantConnectionResolver>()
            .Resolve(serviceName, baseConnectionString);
}
