using System;
using Domium.Caching.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Domium.Caching.Memory;

public static class DomiumCacheStoreOptionsMemoryExtensions
{
    /// <summary>Backs the feature with the in-process memory store.</summary>
    public static DomiumCacheStoreOptions UseMemory(this DomiumCacheStoreOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));

        return options.UseStore(provider => new MemoryDomiumCache(provider.GetRequiredService<IMemoryCache>()));
    }
}
