using System;
using Domium.Caching.Abstractions;
using StackExchange.Redis;

namespace Domium.Caching.Redis;

public static class DomiumCacheStoreOptionsRedisExtensions
{
    /// <summary>
    /// Backs the feature with Redis, connecting lazily with <paramref name="connectionString"/>.
    /// The created multiplexer is owned by the store and disposed with the container.
    /// </summary>
    public static DomiumCacheStoreOptions UseRedis(this DomiumCacheStoreOptions options, string connectionString)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Redis connection string cannot be empty.", nameof(connectionString));
        }

        return options.UseStore(_ => new RedisDomiumCache(
            ConnectionMultiplexer.Connect(connectionString),
            ownsConnection: true));
    }

    /// <summary>Backs the feature with Redis over a caller-supplied multiplexer.</summary>
    public static DomiumCacheStoreOptions UseRedis(
        this DomiumCacheStoreOptions options,
        Func<IServiceProvider, IConnectionMultiplexer> connectionFactory)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (connectionFactory == null) throw new ArgumentNullException(nameof(connectionFactory));

        return options.UseStore(provider => new RedisDomiumCache(connectionFactory(provider)));
    }
}
