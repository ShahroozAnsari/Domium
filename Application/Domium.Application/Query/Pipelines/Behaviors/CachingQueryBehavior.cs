
using Domium.Application.Abstractions.Query;
using Domium.Application.Abstractions.Query.Pipelines;
using Domium.Caching.Abstractions.Providers;
using Domium.Caching.Abstractions.Stores;

namespace Domium.Application.Query.Pipelines.Behaviors;

/// <summary>
/// Provides cache-aware execution for queries when a cache policy exists
/// for the current query type.
/// </summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResult">The query result type.</typeparam>
public sealed class CachingQueryBehavior<TQuery, TResult> : IQueryPipelineBehavior<TQuery, TResult>
    where TQuery : class, IQuery<TResult>
    where TResult : class
{
    private readonly IDomiumCacheStore _cacheStore;
    private readonly IDomiumCacheKeyProvider _cacheKeyProvider;
    private readonly IDomiumQueryCachePolicyProvider _policyProvider;
    private readonly IDomiumCacheScopeProvider _scopeProvider;
    private readonly IDomiumCacheInvalidationMetadataProvider _invalidationMetadataProvider;
    private readonly IDomiumCacheEntryOptionsFactory _entryOptionsFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingQueryBehavior{TQuery, TResult}"/> class.
    /// </summary>
    /// <param name="cacheStore">The cache store.</param>
    /// <param name="cacheKeyProvider">The cache key provider.</param>
    /// <param name="policyProvider">The cache policy provider.</param>
    /// <param name="scopeProvider">The cache scope provider.</param>
    public CachingQueryBehavior(
        IDomiumCacheStore cacheStore,
        IDomiumCacheKeyProvider cacheKeyProvider,
        IDomiumQueryCachePolicyProvider policyProvider,
        IDomiumCacheScopeProvider scopeProvider,
        IDomiumCacheInvalidationMetadataProvider invalidationMetadataProvider,
        IDomiumCacheEntryOptionsFactory entryOptionsFactory)
    {
        _cacheStore = cacheStore ?? throw new ArgumentNullException(nameof(cacheStore));
        _cacheKeyProvider = cacheKeyProvider ?? throw new ArgumentNullException(nameof(cacheKeyProvider));
        _policyProvider = policyProvider ?? throw new ArgumentNullException(nameof(policyProvider));
        _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
        _invalidationMetadataProvider = invalidationMetadataProvider ?? throw new ArgumentNullException(nameof(invalidationMetadataProvider));
        _entryOptionsFactory = entryOptionsFactory ?? throw new ArgumentNullException(nameof(entryOptionsFactory));
    }

    /// <summary>
    /// Executes the query using the configured cache policy for the query type.
    /// </summary>
    /// <param name="query">The query instance.</param>
    /// <param name="next">The next delegate in the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The cached or computed query result.</returns>
    public async Task<TResult> HandleAsync(
        TQuery query,
        CancellationToken cancellationToken,
        QueryHandlerDelegate<TResult> next)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(next);

        var policy = _policyProvider.GetPolicy(typeof(TQuery));

        if (policy is null || !policy.Enabled)
        {
            return await next().ConfigureAwait(false);
        }

        var scope = _scopeProvider.GetScope(policy);

        var cacheKey = _cacheKeyProvider.CreateKey(query, policy, scope);

        var cached = await _cacheStore
            .TryGetAsync<TResult>(cacheKey, cancellationToken)
            .ConfigureAwait(false);

        if (cached.HasValue)
        {
            return cached.Value!;
        }

        var result = await next().ConfigureAwait(false);

        if (result is null && !policy.CacheNullValues)
        {
            return result!;
        }

        var invalidationMetadata = _invalidationMetadataProvider.GetMetadata(query, policy);
        var entryOptions = _entryOptionsFactory.Create(policy);

        await _cacheStore.SetAsync(
            cacheKey,
            result,
            entryOptions,
            invalidationMetadata,
            cancellationToken).ConfigureAwait(false);


        return result!;

    }
}