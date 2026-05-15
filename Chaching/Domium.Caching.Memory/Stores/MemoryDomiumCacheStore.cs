using System;
using System.Threading;
using System.Threading.Tasks;
using Domium.Caching.Abstractions.Models;
using Domium.Caching.Abstractions.Results;
using Domium.Caching.Abstractions.Stores;
using Domium.Caching.Memory.Internal;
using Microsoft.Extensions.Caching.Memory;

namespace Domium.Caching.Memory.Stores
{
    /// <summary>
    /// Represents an in-memory implementation of <see cref="IDomiumCacheStore"/>.
    /// </summary>
    public sealed class MemoryDomiumCacheStore : IDomiumCacheStore
    {
        private readonly IMemoryCache _memoryCache;
        private readonly MemoryCacheIndex _index;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryDomiumCacheStore"/> class.
        /// </summary>
        /// <param name="memoryCache">
        /// The memory cache instance.
        /// </param>
        public MemoryDomiumCacheStore(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _index = new MemoryCacheIndex();
        }

        /// <summary>
        /// Attempts to retrieve a cached value by key.
        /// </summary>
        /// <typeparam name="T">
        /// The cached value type.
        /// </typeparam>
        /// <param name="key">
        /// The cache key.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token that can cancel the operation.
        /// </param>
        /// <returns>
        /// A cache hit or miss result.
        /// </returns>
        public Task<DomiumCacheResult<T>> TryGetAsync<T>(
            string key,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));
            }

            if (_memoryCache.TryGetValue(key, out var raw) && raw is MemoryDomiumCacheEnvelope<T> envelope && envelope.HasValue)
            {
                return Task.FromResult(DomiumCacheResult<T>.Hit(envelope.Value));
            }

            return Task.FromResult(DomiumCacheResult<T>.Miss());
        }

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
        /// Cache entry expiration options.
        /// </param>
        /// <param name="invalidationMetadata">
        /// Metadata used to support cache invalidation.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token that can cancel the operation.
        /// </param>
        public Task SetAsync<T>(
            string key,
            T value,
            DomiumCacheEntryOptions options,
            DomiumCacheInvalidationMetadata invalidationMetadata,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));
            }

            var entryOptions = new MemoryCacheEntryOptions();

            if (options != null)
            {
                if (options.AbsoluteExpirationRelativeToNow.HasValue)
                {
                    entryOptions.AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow;
                }

                if (options.SlidingExpiration.HasValue)
                {
                    entryOptions.SlidingExpiration = options.SlidingExpiration;
                }
            }

            var metadata = invalidationMetadata ?? new DomiumCacheInvalidationMetadata(null, null, null);
            var envelope = new MemoryDomiumCacheEnvelope<T>(true, value, metadata);

            _memoryCache.Set(key, envelope, entryOptions);

            _index.AddTags(key, metadata.Tags);
            _index.AddEntityKeys(key, metadata.EntityKeys);
            _index.AddGroup(key, metadata.Group);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Removes a cache entry by key.
        /// </summary>
        /// <param name="key">
        /// The cache key.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token that can cancel the operation.
        /// </param>
        public Task RemoveAsync(
            string key,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(key))
            {
                return Task.CompletedTask;
            }

            _memoryCache.Remove(key);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Removes cache entries associated with the specified tag.
        /// </summary>
        /// <param name="tag">
        /// The tag to invalidate.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token that can cancel the operation.
        /// </param>
        public Task RemoveByTagAsync(
            string tag,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var key in _index.GetKeysByTag(tag))
            {
                _memoryCache.Remove(key);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Removes cache entries associated with the specified entity key.
        /// </summary>
        /// <param name="entityKey">
        /// The entity key to invalidate.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token that can cancel the operation.
        /// </param>
        public Task RemoveByEntityKeyAsync(
            string entityKey,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var key in _index.GetKeysByEntityKey(entityKey))
            {
                _memoryCache.Remove(key);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Removes cache entries associated with the specified group.
        /// </summary>
        /// <param name="group">
        /// The group to invalidate.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token that can cancel the operation.
        /// </param>
        public Task RemoveByGroupAsync(
            string group,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var key in _index.GetKeysByGroup(group))
            {
                _memoryCache.Remove(key);
            }

            return Task.CompletedTask;
        }
    }
}
