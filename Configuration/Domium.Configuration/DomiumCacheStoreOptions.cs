using StackExchange.Redis;

namespace Domium.Configuration;

public sealed class DomiumCacheStoreOptions
{
    public DomiumCacheProvider Provider { get; set; } = DomiumCacheProvider.Memory;

    public string RedisConnectionString { get; set; } = "localhost";

    public Func<IServiceProvider, IConnectionMultiplexer>? RedisConnectionFactory { get; set; }

    public DomiumCacheStoreOptions UseMemory()
    {
        Provider = DomiumCacheProvider.Memory;
        return this;
    }

    public DomiumCacheStoreOptions UseRedis(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Redis connection string cannot be empty.", nameof(connectionString));
        }

        Provider = DomiumCacheProvider.Redis;
        RedisConnectionString = connectionString;
        RedisConnectionFactory = null;
        return this;
    }

    public DomiumCacheStoreOptions UseRedis(Func<IServiceProvider, IConnectionMultiplexer> connectionFactory)
    {
        RedisConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        Provider = DomiumCacheProvider.Redis;
        return this;
    }
}
