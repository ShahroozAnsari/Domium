using Domium.Caching.Abstractions.Models;

namespace Domium.Caching.Abstractions.Providers;

/// <summary>
/// Creates deterministic cache keys for queries.
/// </summary>
public interface IDomiumCacheKeyProvider
{
    /// <summary>
    /// Creates a cache key for the specified query and scope.
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
    /// <param name="scope">
    /// The resolved cache scope.
    /// </param>
    /// <returns>
    /// A deterministic cache key.
    /// </returns>
    string CreateKey<TQuery>(
        TQuery query,
        DomiumQueryCachePolicy policy,
        DomiumCacheScope scope)
        where TQuery : class;
}