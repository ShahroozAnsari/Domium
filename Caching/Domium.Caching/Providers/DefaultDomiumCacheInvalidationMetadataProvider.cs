using System;
using Domium.Caching.Abstractions.Models;
using Domium.Caching.Abstractions.Providers;

namespace Domium.Caching.Providers;

/// <summary>
/// Provides invalidation metadata for cache entries.
/// </summary>
public sealed class DefaultDomiumCacheInvalidationMetadataProvider : IDomiumCacheInvalidationMetadataProvider
{
    /// <summary>
    /// Gets invalidation metadata for the specified query and policy.
    /// </summary>
    /// <typeparam name="TQuery">
    /// The query type.
    /// </typeparam>
    /// <param name="query">
    /// The query instance.
    /// </param>
    /// <param name="policy">
    /// The applied cache policy.
    /// </param>
    /// <returns>
    /// A <see cref="DomiumCacheInvalidationMetadata"/> instance.
    /// </returns>
    public DomiumCacheInvalidationMetadata GetMetadata<TQuery>(
        TQuery query,
        DomiumQueryCachePolicy policy)
        where TQuery : class
    {
        if (policy == null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        return policy.InvalidationMetadata
               ?? new DomiumCacheInvalidationMetadata(null, null, null);
    }
}