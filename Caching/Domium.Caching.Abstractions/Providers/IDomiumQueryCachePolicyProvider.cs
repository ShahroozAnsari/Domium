using System;
using Domium.Caching.Abstractions.Models;

namespace Domium.Caching.Abstractions.Providers;

/// <summary>
/// Provides cache policies for query types.
/// </summary>
public interface IDomiumQueryCachePolicyProvider
{
    /// <summary>
    /// Gets the cache policy for the specified query type.
    /// </summary>
    /// <param name="queryType">
    /// The query CLR type.
    /// </param>
    /// <returns>
    /// The cache policy when found; otherwise, <c>null</c>.
    /// </returns>
    DomiumQueryCachePolicy? GetPolicy(Type queryType);
}