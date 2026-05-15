using System;
using System.Threading;
using System.Threading.Tasks;
using Domium.Caching.Abstractions.Providers;
using Domium.Caching.Abstractions.Stores;

namespace Domium.Caching.Behaviors
{
    /// <summary>
    /// Provides query-result caching behavior for the Domium query pipeline.
    /// </summary>
    /// <typeparam name="TQuery">
    /// The query type.
    /// </typeparam>
    /// <typeparam name="TResult">
    /// The query result type.
    /// </typeparam>
    public sealed class CachingQueryBehavior<TQuery, TResult> : IDomiumQueryBehavior<TQuery, TResult>
        where TQuery : class
    {
        private readonly IDomiumQueryCachePolicyProvider _policyProvider;
        private readonly IDomiumCacheScopeProvider _scopeProvider;
        private readonly IDomiumCacheKeyProvider _keyProvider;
        private readonly IDomiumCacheInvalidationMetadataProvider _metadataProvider;
        private readonly IDomiumCacheEntryOptionsFactory _entryOptionsFactory;
        private readonly IDomiumCacheStore _cacheStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="CachingQueryBehavior{TQuery, TResult}"/> class.
        /// </summary>
        /// <param name="policyProvider">
        /// The cache policy provider.
        /// </param>
        /// <param name="scopeProvider">
        /// The cache scope provider.
        /// </param>
        /// <param name="keyProvider">
        /// The cache key provider.
        /// </param>
        /// <param name="metadataProvider">
        /// The invalidation metadata provider.
        /// </param>
        /// <param name="entryOptionsFactory">
        /// The cache entry options factory.
        /// </param>
        /// <param name="cacheStore">
        /// The cache store.
        /// </param>
        public CachingQueryBehavior(
            IDomiumQueryCachePolicyProvider policyProvider,
            IDomiumCacheScopeProvider scopeProvider,
            IDomiumCacheKeyProvider keyProvider,
            IDomiumCacheInvalidationMetadataProvider metadataProvider,
            IDomiumCacheEntryOptionsFactory entryOptionsFactory,
            IDomiumCacheStore cacheStore)
        {
            _policyProvider = policyProvider ?? throw new ArgumentNullException(nameof(policyProvider));
            _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
            _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
            _metadataProvider = metadataProvider ?? throw new ArgumentNullException(nameof(metadataProvider));
            _entryOptionsFactory = entryOptionsFactory ?? throw new ArgumentNullException(nameof(entryOptionsFactory));
            _cacheStore = cacheStore ?? throw new ArgumentNullException(nameof(cacheStore));
        }

        /// <summary>
        /// Handles the query using cache when a policy exists and caching is enabled.
        /// </summary>
        /// <param name="query">
        /// The query instance.
        /// </param>
        /// <param name="next">
        /// The delegate that executes the next pipeline component.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token that can cancel the operation.
        /// </param>
        /// <returns>
        /// The query result.
        /// </returns>
        public async Task<TResult> HandleAsync(
            TQuery query,
            DomiumQueryHandlerDelegate<TResult> next,
            CancellationToken cancellationToken)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            var policy = _policyProvider.GetPolicy(typeof(TQuery));

            if (policy == null || !policy.Enabled)
            {
                return await next().ConfigureAwait(false);
            }

            var scope = _scopeProvider.GetScope(policy);
            var key = _keyProvider.CreateKey(query, policy, scope);
            var cachedResult = await _cacheStore.TryGetAsync<TResult>(key, cancellationToken).ConfigureAwait(false);

            if (cachedResult.HasValue)
            {
                return cachedResult.Value;
            }

            var result = await next().ConfigureAwait(false);

            if (result == null && !policy.CacheNullValues)
            {
                return result;
            }

            var options = _entryOptionsFactory.Create(policy);
            var metadata = _metadataProvider.GetMetadata(query, policy);

            await _cacheStore.SetAsync(
                key,
                result,
                options,
                metadata,
                cancellationToken).ConfigureAwait(false);

            return result;
        }
    }
}
