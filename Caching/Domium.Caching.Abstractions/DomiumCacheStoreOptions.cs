using System;

namespace Domium.Caching.Abstractions;

/// <summary>
/// Selects which <see cref="IDomiumCache"/> store backs a Domium feature (query caching,
/// idempotency). Providers contribute extension methods — <c>UseMemory()</c> from
/// Domium.Caching.Memory, <c>UseRedis(...)</c> from Domium.Caching.Redis — so applications
/// only reference the store they actually use. When nothing is configured the composition
/// falls back to the in-memory store.
/// </summary>
public sealed class DomiumCacheStoreOptions
{
    /// <summary>The factory that creates the store, or <c>null</c> for the default (memory).</summary>
    public Func<IServiceProvider, IDomiumCache>? StoreFactory { get; private set; }

    /// <summary>Registers a custom store factory. Providers call this from their Use* extensions.</summary>
    public DomiumCacheStoreOptions UseStore(Func<IServiceProvider, IDomiumCache> storeFactory)
    {
        StoreFactory = storeFactory ?? throw new ArgumentNullException(nameof(storeFactory));
        return this;
    }
}
