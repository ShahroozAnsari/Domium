using Microsoft.Extensions.DependencyInjection;

namespace Domium.Persistence.Dapper;

/// <summary>
/// Configures Domium Dapper persistence.
/// </summary>
public sealed class DomiumDapperOptions
{
    internal Action<IServiceCollection>? RegisterConnectionFactory { get; private set; }

    internal bool AggregateRepositoriesEnabled { get; private set; }

    public DomiumDapperOptions UseConnectionFactory<TConnectionFactory>()
        where TConnectionFactory : class, IDapperConnectionFactory
    {
        RegisterConnectionFactory = services =>
        {
            services.AddScoped<TConnectionFactory>();
            services.AddScoped<IDapperConnectionFactory>(
                provider => provider.GetRequiredService<TConnectionFactory>());
        };

        return this;
    }

    public DomiumDapperOptions UseConnectionFactory(
        Func<IServiceProvider, IDapperConnectionFactory> factory)
    {
        if (factory == null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        RegisterConnectionFactory = services => services.AddScoped(factory);

        return this;
    }

    public DomiumDapperOptions UseConnectionFactory(IDapperConnectionFactory connectionFactory)
    {
        if (connectionFactory == null)
        {
            throw new ArgumentNullException(nameof(connectionFactory));
        }

        RegisterConnectionFactory = services => services.AddSingleton(connectionFactory);

        return this;
    }

    public DomiumDapperOptions UseAggregateRepositories()
    {
        AggregateRepositoriesEnabled = true;
        return this;
    }
}
