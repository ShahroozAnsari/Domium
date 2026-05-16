using Domium.Caching.Abstractions.Models;

namespace Domium.Caching.Abstractions.Providers;

/// <summary>
/// Resolves invalidation metadata for a query instance.
/// </summary>
public interface IDomiumCacheInvalidationMetadataProvider
{
    /// <summary>
    /// Gets invalidation metadata for the specified query.
    /// </summary>
    /// <typeparam name="TQuery">
    /// The query type.
    /// </typeparam>
    /// <param name="query">
    /// The query instance.
    /// </param>
    /// <param name="policy">
    /// The cache policy applied to the query.
    /// </param>
    /// <returns>
    /// The invalidation metadata for the cache entry.
    /// </returns>
    DomiumCacheInvalidationMetadata GetMetadata<TQuery>(
        TQuery query,
        DomiumQueryCachePolicy policy)
        where TQuery : class;
}