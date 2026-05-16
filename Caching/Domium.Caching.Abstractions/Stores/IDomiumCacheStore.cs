using System.Threading;
using System.Threading.Tasks;
using Domium.Caching.Abstractions.Models;
using Domium.Caching.Abstractions.Results;

namespace Domium.Caching.Abstractions.Stores;

/// <summary>
/// Represents a provider-independent cache store.
/// </summary>
public interface IDomiumCacheStore
{
    /// <summary>
    /// Attempts to retrieve a cached value by key.
    /// </summary>
    /// <typeparam name="T">
    /// The expected cached value type.
    /// </typeparam>
    /// <param name="key">
    /// The cache key.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can cancel the operation.
    /// </param>
    /// <returns>
    /// A cache result representing a hit or miss.
    /// </returns>
    Task<DomiumCacheResult<T>> TryGetAsync<T>(
        string key,
        CancellationToken cancellationToken);

    /// <summary>
    /// Stores a value in the cache.
    /// </summary>
    /// <typeparam name="T">
    /// The cached value type.
    /// </typeparam>
    /// <param name="key">
    /// The cache key.
    /// </param>
    /// <param name="value">
    /// The value to cache.
    /// </param>
    /// <param name="options">
    /// Expiration options for the cache entry.
    /// </param>
    /// <param name="invalidationMetadata">
    /// Metadata used to support cache invalidation.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can cancel the operation.
    /// </param>
    Task SetAsync<T>(
        string key,
        T value,
        DomiumCacheEntryOptions options,
        DomiumCacheInvalidationMetadata invalidationMetadata,
        CancellationToken cancellationToken);

    /// <summary>
    /// Removes a cache entry by key.
    /// </summary>
    /// <param name="key">
    /// The cache key.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can cancel the operation.
    /// </param>
    Task RemoveAsync(
        string key,
        CancellationToken cancellationToken);

    /// <summary>
    /// Removes cache entries associated with the specified tag.
    /// </summary>
    /// <param name="tag">
    /// The tag to invalidate.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can cancel the operation.
    /// </param>
    Task RemoveByTagAsync(
        string tag,
        CancellationToken cancellationToken);

    /// <summary>
    /// Removes cache entries associated with the specified entity key.
    /// </summary>
    /// <param name="entityKey">
    /// The entity key to invalidate.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can cancel the operation.
    /// </param>
    Task RemoveByEntityKeyAsync(
        string entityKey,
        CancellationToken cancellationToken);

    /// <summary>
    /// Removes cache entries associated with the specified group.
    /// </summary>
    /// <param name="group">
    /// The group to invalidate.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can cancel the operation.
    /// </param>
    Task RemoveByGroupAsync(
        string group,
        CancellationToken cancellationToken);
}