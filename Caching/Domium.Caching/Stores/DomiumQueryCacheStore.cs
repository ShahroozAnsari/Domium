using Domium.Caching.Abstractions.Stores;

namespace Domium.Caching.Stores;

public sealed class DomiumQueryCacheStore(IDomiumCacheStore inner)
    : DomiumCacheStoreAdapter(inner), IDomiumQueryCacheStore
{
}
