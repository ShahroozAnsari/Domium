namespace Domium.Configuration;

public sealed class DomiumCachingOptions
{
    public DomiumCacheStoreOptions Store { get; } = new();

    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);
}
