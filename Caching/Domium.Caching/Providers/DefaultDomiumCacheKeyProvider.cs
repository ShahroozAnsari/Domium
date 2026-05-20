using System;
using Domium.Caching.Abstractions.Models;
using Domium.Caching.Abstractions.Providers;

namespace Domium.Caching.Providers;

/// <summary>
/// Creates deterministic cache keys for query instances.
/// </summary>
public sealed class DefaultDomiumCacheKeyProvider(IDomiumCacheKeyFactory keyFactory) : IDomiumCacheKeyProvider
{
    private readonly IDomiumCacheKeyFactory _keyFactory =
        keyFactory ?? throw new ArgumentNullException(nameof(keyFactory));

    /// <summary>
    /// Creates a deterministic cache key for the specified query and scope.
    /// </summary>
    /// <typeparam name="TQuery">
    /// The query type.
    /// </typeparam>
    /// <param name="query">
    /// The query instance.
    /// </param>
    /// <param name="policy">
    /// The cache policy.
    /// </param>
    /// <param name="scope">
    /// The resolved cache scope.
    /// </param>
    /// <returns>
    /// A deterministic cache key.
    /// </returns>
    public string CreateKey<TQuery>(
        TQuery query,
        DomiumQueryCachePolicy policy,
        DomiumCacheScope scope)
        where TQuery : class
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (policy == null) throw new ArgumentNullException(nameof(policy));
        if (scope == null) throw new ArgumentNullException(nameof(scope));

        var prefix = string.IsNullOrWhiteSpace(policy.KeyPrefix)
            ? "domium"
            : policy.KeyPrefix.Trim();

        var scopeSegment = scope.Kind == DomiumCacheScopeKind.Global
            ? "global"
            : $"tenant:{scope.TenantId}";

        return _keyFactory.CreateKey(
            new DomiumCacheKeyDescriptor(
                prefix,
                "query",
                typeof(TQuery).FullName ?? typeof(TQuery).Name,
                scopeSegment,
                query));
    }
}
