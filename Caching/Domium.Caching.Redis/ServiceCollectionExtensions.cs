using System;
using Domium.Caching.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace Domium.Caching.Redis;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers the Redis <see cref="IDomiumCache"/> over an existing multiplexer registration.</summary>
    public static IServiceCollection AddDomiumRedisCache(this IServiceCollection services)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        services.TryAddSingleton<IDomiumCache>(provider =>
            new RedisDomiumCache(provider.GetRequiredService<IConnectionMultiplexer>()));

        return services;
    }

    /// <summary>Registers a multiplexer for <paramref name="connectionString"/> and the Redis cache.</summary>
    public static IServiceCollection AddDomiumRedisCache(this IServiceCollection services, string connectionString)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Redis connection string cannot be empty.", nameof(connectionString));
        }

        services.TryAddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(connectionString));
        return services.AddDomiumRedisCache();
    }
}
