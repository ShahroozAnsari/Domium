using Domium.Application.Abstractions.Query;
using Domium.Application.Abstractions.Query.Pipelines;
using Domium.Caching.Abstractions;

namespace Domium.Application.Query.Pipelines.Behaviors;

/// <summary>
/// Caches results of queries that opt in by implementing <see cref="ICacheableQuery"/>.
/// The key is derived from the query type and its JSON payload; the duration and
/// invalidation tags come from the query itself (falling back to the configured default).
/// </summary>
public sealed class CachingQueryBehavior<TQuery, TResult>(
    IDomiumCache cache,
    DomiumQueryCachingOptions options) : IQueryPipelineBehavior<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    public async Task<TResult> HandleAsync(
        TQuery query,
        CancellationToken cancellationToken,
        QueryHandlerDelegate<TResult> next)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (next == null) throw new ArgumentNullException(nameof(next));

        if (query is not ICacheableQuery cacheable)
        {
            return await next().ConfigureAwait(false);
        }

        var key = DomiumCacheKeyBuilder.BuildQueryKey(query);

        var cached = await cache.GetAsync<TResult>(key, cancellationToken).ConfigureAwait(false);
        if (cached.Found)
        {
            return cached.Value!;
        }

        var result = await next().ConfigureAwait(false);

        if (result is not null)
        {
            var entryOptions = new DomiumCacheEntryOptions(
                cacheable.Duration ?? options.DefaultDuration,
                cacheable.Tags);

            await cache.SetAsync(key, result, entryOptions, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }
}
