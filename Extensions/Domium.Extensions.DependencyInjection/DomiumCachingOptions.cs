namespace Domium.Extensions.DependencyInjection;

public sealed class DomiumCachingOptions
{
    public DomiumCacheProvider Provider { get; set; } = DomiumCacheProvider.Memory;

    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);

    public string RedisConnectionString { get; set; } = "localhost";
}