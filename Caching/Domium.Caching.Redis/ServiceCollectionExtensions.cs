using System;
using Domium.Caching.Abstractions.Stores;
using Domium.Caching.Redis.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace Domium.Caching.Redis;

/// <summary>
/// Dependency injection helpers for Redis-backed Domium caching.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Redis cache store used by Domium query caching.
    /// </summary>
    public static IServiceCollection AddDomiumRedisCacheStore(
        this IServiceCollection services,
        string connectionString)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Redis connection string cannot be empty.", nameof(connectionString));
        }

        services.TryAddSingleton<IConnectionMultiplexer>(
            _ => ConnectionMultiplexer.Connect(connectionString));

        services.TryAddSingleton<IDomiumCacheStore, RedisDomiumCacheStore>();

        return services;
    }

    /// <summary>
    /// Registers the Redis cache store using an existing connection multiplexer.
    /// </summary>
    public static IServiceCollection AddDomiumRedisCacheStore(
        this IServiceCollection services,
        IConnectionMultiplexer connectionMultiplexer)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (connectionMultiplexer == null)
        {
            throw new ArgumentNullException(nameof(connectionMultiplexer));
        }

        services.TryAddSingleton(connectionMultiplexer);
        services.TryAddSingleton<IDomiumCacheStore, RedisDomiumCacheStore>();

        return services;
    }
}
