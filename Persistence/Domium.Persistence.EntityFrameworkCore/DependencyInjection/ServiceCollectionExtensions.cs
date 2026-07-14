using Domium.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Domium.Persistence.EntityFrameworkCore;

/// <summary>
/// Dependency injection helpers for Domium EF Core persistence.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a DbContext and Domium repositories/unit of work in one call.
    /// </summary>
    public static IServiceCollection AddDomiumEntityFrameworkCore<TDbContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
        where TDbContext : DbContext
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configureDbContext == null)
        {
            throw new ArgumentNullException(nameof(configureDbContext));
        }

        services.AddDbContext<TDbContext>((provider, options) =>
        {
            configureDbContext(options);
            AddDomiumInterceptors(provider, options);
        });

        return services.AddDomiumEntityFrameworkCoreCore<TDbContext>();
    }

    /// <summary>
    /// Registers a DbContext and Domium repositories/unit of work in one call.
    /// </summary>
    public static IServiceCollection AddDomiumEntityFrameworkCore<TDbContext>(
        this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder> configureDbContext)
        where TDbContext : DbContext
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configureDbContext == null)
        {
            throw new ArgumentNullException(nameof(configureDbContext));
        }

        services.AddDbContext<TDbContext>((provider, options) =>
        {
            configureDbContext(provider, options);
            AddDomiumInterceptors(provider, options);
        });

        return services.AddDomiumEntityFrameworkCoreCore<TDbContext>();
    }

    /// <summary>
    /// Registers Domium repositories and unit of work for a DbContext already registered with DI.
    /// </summary>
    public static IServiceCollection AddDomiumEntityFrameworkCore<TDbContext>(
        this IServiceCollection services)
        where TDbContext : DbContext
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddDbContext<TDbContext>((provider, options) =>
            AddDomiumInterceptors(provider, options));

        return services.AddDomiumEntityFrameworkCoreCore<TDbContext>();
    }

    private static IServiceCollection AddDomiumEntityFrameworkCoreCore<TDbContext>(
        this IServiceCollection services)
        where TDbContext : DbContext
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.TryAddScoped<DbContext>(
            provider => provider.GetRequiredService<TDbContext>());

        if (typeof(DomiumDbContext).IsAssignableFrom(typeof(TDbContext)))
        {
            services.TryAddScoped(
                typeof(DomiumDbContext),
                provider => provider.GetRequiredService<TDbContext>());
        }

        services.TryAddScoped<IUnitOfWork, EfUnitOfWork>();

        services.TryAddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));
        services.TryAddScoped(typeof(ISpecificationRepository<,>), typeof(EfRepository<,>));
        services.TryAddSingleton<TimeProvider>(_ => TimeProvider.System);
        services.TryAddScoped<IDomiumCurrentUserAccessor, NullDomiumCurrentUserAccessor>();
        services.TryAddScoped<SoftDeleteSaveChangesInterceptor>();
        services.TryAddScoped<AuditableSaveChangesInterceptor>();
        services.TryAddScoped<ConcurrencyVersionSaveChangesInterceptor>();
        services.TryAddScoped<DomainServiceMaterializationInterceptor>();

        return services;
    }

    private static void AddDomiumInterceptors(
        IServiceProvider provider,
        DbContextOptionsBuilder options)
    {
        options.AddInterceptors(
            provider.GetRequiredService<SoftDeleteSaveChangesInterceptor>(),
            provider.GetRequiredService<AuditableSaveChangesInterceptor>(),
            provider.GetRequiredService<ConcurrencyVersionSaveChangesInterceptor>(),
            provider.GetRequiredService<DomainServiceMaterializationInterceptor>());
    }
}
