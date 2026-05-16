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

        services.TryAddScoped<DbContext>(
            provider => provider.GetRequiredService<TDbContext>());

        services.TryAddScoped<IUnitOfWork, EfUnitOfWork>();
        services.TryAddScoped(typeof(IReadOnlyRepository<,>), typeof(EfRepository<,>));
        services.TryAddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));

        return services;
    }
}
