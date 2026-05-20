using Domium.Caching.Abstractions.Stores;

namespace Domium.Caching.Stores;

public sealed class DomiumIdempotencyCacheStore(IDomiumCacheStore inner)
    : DomiumCacheStoreAdapter(inner), IDomiumIdempotencyCacheStore
{
}
