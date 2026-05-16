using Domium.Caching.Abstractions.Models;

namespace Domium.Caching.Abstractions.Providers;

/// <summary>
/// Resolves the runtime cache scope for a query cache policy.
/// </summary>
public interface IDomiumCacheScopeProvider
{
    /// <summary>
    /// Resolves the cache scope for the specified policy.
    /// </summary>
    /// <param name="policy">
    /// The query cache policy.
    /// </param>
    /// <returns>
    /// The resolved cache scope.
    /// </returns>
    DomiumCacheScope GetScope(DomiumQueryCachePolicy policy);
}