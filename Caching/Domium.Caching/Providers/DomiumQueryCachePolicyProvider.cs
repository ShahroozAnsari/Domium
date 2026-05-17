using System;
using System.Collections.Concurrent;
using Domium.Caching.Abstractions.Models;
using Domium.Caching.Abstractions.Providers;

namespace Domium.Caching.Providers;

/// <summary>
/// Represents an in-memory registry of query cache policies.
/// </summary>
public sealed class DomiumQueryCachePolicyProvider :
    IDomiumQueryCachePolicyProvider,
    IDomiumQueryCachePolicyRegistry
{
    private readonly ConcurrentDictionary<Type, DomiumQueryCachePolicy> _policies;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomiumQueryCachePolicyProvider"/> class.
    /// </summary>
    public DomiumQueryCachePolicyProvider()
    {
        _policies = new ConcurrentDictionary<Type, DomiumQueryCachePolicy>();
    }

    /// <summary>
    /// Registers a cache policy.
    /// </summary>
    /// <param name="policy">
    /// The policy to register.
    /// </param>
    public void Register(DomiumQueryCachePolicy policy)
    {
        if (policy == null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        _policies[policy.QueryType] = policy;
    }

    /// <summary>
    /// Gets the cache policy for the specified query type.
    /// </summary>
    /// <param name="queryType">
    /// The query CLR type.
    /// </param>
    /// <returns>
    /// The registered policy when found; otherwise, <c>null</c>.
    /// </returns>
    public DomiumQueryCachePolicy? GetPolicy(Type queryType)
    {
        if (queryType == null)
        {
            throw new ArgumentNullException(nameof(queryType));
        }

        _policies.TryGetValue(queryType, out var policy);
        return policy;
    }
}
