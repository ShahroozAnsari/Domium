using Domium.Persistence.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Domium.Persistence.Dapper;

/// <summary>
/// Dependency injection helpers for Domium Dapper persistence.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDomiumDapper(
        this IServiceCollection services,
        Action<DomiumDapperOptions> configure)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var options = new DomiumDapperOptions();
        configure(options);

        if (options.RegisterConnectionFactory is null)
        {
            throw new InvalidOperationException(
                "Dapper persistence requires a connection factory. Call UseConnectionFactory.");
        }

        options.RegisterConnectionFactory(services);

        services.TryAddScoped<DapperSession>();
        services.TryAddScoped<IDapperSession>(
            provider => provider.GetRequiredService<DapperSession>());
        services.TryAddScoped<IDapperSqlExecutor, DapperSqlExecutor>();
        services.TryAddScoped<IUnitOfWork, DapperUnitOfWork>();

        if (options.AggregateRepositoriesEnabled)
        {
            services.TryAddScoped(typeof(IRepository<,>), typeof(DapperRepository<,>));
        }

        return services;
    }

    public static IServiceCollection AddDomiumDapper<TConnectionFactory>(
        this IServiceCollection services)
        where TConnectionFactory : class, IDapperConnectionFactory
    {
        return services.AddDomiumDapper(options =>
            options.UseConnectionFactory<TConnectionFactory>());
    }
}
