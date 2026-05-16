using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Domium.Caching.Abstractions.Models;
using Domium.Caching.Abstractions.Results;
using Domium.Caching.Abstractions.Stores;
using Domium.Caching.Redis.Internal;
using StackExchange.Redis;

namespace Domium.Caching.Redis.Stores
{
    /// <summary>
    /// Represents a Redis implementation of <see cref="IDomiumCacheStore"/>.
    /// </summary>
    public sealed class RedisDomiumCacheStore : IDomiumCacheStore
    {
        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = false
        };

        private readonly IDatabase _database;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisDomiumCacheStore"/> class.
        /// </summary>
        /// <param name="connectionMultiplexer">
        /// The Redis connection multiplexer.
        /// </param>
        public RedisDomiumCacheStore(IConnectionMultiplexer connectionMultiplexer)
        {
            if (connectionMultiplexer == null)
            {
                throw new ArgumentNullException(nameof(connectionMultiplexer));
            }

            _database = connectionMultiplexer.GetDatabase();
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
        public async Task<DomiumCacheResult<T>> TryGetAsync<T>(
            string key,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));
            }

            var redisValue = await _database.StringGetAsync(key).ConfigureAwait(false);

            if (!redisValue.HasValue)
            {
                return DomiumCacheResult<T>.Miss();
            }

            var payload = redisValue.ToString();

            if (string.IsNullOrWhiteSpace(payload))
            {
                return DomiumCacheResult<T>.Miss();
            }

            var envelope = JsonSerializer.Deserialize<RedisDomiumCacheEnvelope<T>>(payload, SerializerOptions);

            if (envelope == null || !envelope.HasValue)
            {
                return DomiumCacheResult<T>.Miss();
            }

            return DomiumCacheResult<T>.Hit(envelope.Value);
        }

        /// <summary>
        /// Stores a value in Redis.
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
        /// The cache entry options.
        /// </param>
        /// <param name="invalidationMetadata">
        /// The invalidation metadata.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token that can cancel the operation.
        /// </param>
        public async Task SetAsync<T>(
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

            var metadata = invalidationMetadata ?? new DomiumCacheInvalidationMetadata(null, null, null);

            var envelope = new RedisDomiumCacheEnvelope<T>
            {
                HasValue = true,
                Value = value,
                Metadata = metadata
            };

            var payload = JsonSerializer.Serialize(envelope, SerializerOptions);
            var ttl = options?.AbsoluteExpirationRelativeToNow;

            await _database.StringSetAsync(
                key,
                payload,
                expiry: ttl.HasValue ? (Expiration)ttl.Value : default
            );



            await AddIndexesAsync(key, metadata).ConfigureAwait(false);
        }

        /// <summary>
        /// Removes a Redis cache entry by key.
        /// </summary>
        /// <param name="key">
        /// The cache key.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token that can cancel the operation.
        /// </param>
        public async Task RemoveAsync(
            string key,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            await _database.KeyDeleteAsync(key).ConfigureAwait(false);
        }

        /// <summary>
        /// Removes Redis cache entries associated with the specified tag.
        /// </summary>
        /// <param name="tag">
        /// The tag to invalidate.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token that can cancel the operation.
        /// </param>
        public async Task RemoveByTagAsync(
            string tag,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(tag))
            {
                return;
            }

            var indexKey = RedisInvalidationIndexKeys.Tag(tag.Trim());
            await RemoveIndexedEntriesAsync(indexKey).ConfigureAwait(false);
        }

        /// <summary>
        /// Removes Redis cache entries associated with the specified entity key.
        /// </summary>
        /// <param name="entityKey">
        /// The entity key to invalidate.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token that can cancel the operation.
        /// </param>
        public async Task RemoveByEntityKeyAsync(
            string entityKey,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(entityKey))
            {
                return;
            }

            var indexKey = RedisInvalidationIndexKeys.EntityKey(entityKey.Trim());
            await RemoveIndexedEntriesAsync(indexKey).ConfigureAwait(false);
        }

        /// <summary>
        /// Removes Redis cache entries associated with the specified group.
        /// </summary>
        /// <param name="group">
        /// The group to invalidate.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token that can cancel the operation.
        /// </param>
        public async Task RemoveByGroupAsync(
            string group,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(group))
            {
                return;
            }

            var indexKey = RedisInvalidationIndexKeys.Group(group.Trim());
            await RemoveIndexedEntriesAsync(indexKey).ConfigureAwait(false);
        }

        private async Task AddIndexesAsync(
            string key,
            DomiumCacheInvalidationMetadata metadata)
        {
            if (metadata == null)
            {
                return;
            }

            if (metadata.Tags != null)
            {
                foreach (var tag in metadata.Tags.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    await _database.SetAddAsync(RedisInvalidationIndexKeys.Tag(tag.Trim()), key).ConfigureAwait(false);
                }
            }

            if (metadata.EntityKeys != null)
            {
                foreach (var entityKey in metadata.EntityKeys.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    await _database.SetAddAsync(RedisInvalidationIndexKeys.EntityKey(entityKey.Trim()), key).ConfigureAwait(false);
                }
            }

            if (!string.IsNullOrWhiteSpace(metadata.Group))
            {
                await _database.SetAddAsync(RedisInvalidationIndexKeys.Group(metadata.Group.Trim()), key).ConfigureAwait(false);
            }
        }

        private async Task RemoveIndexedEntriesAsync(string indexKey)
        {
            var members = await _database.SetMembersAsync(indexKey).ConfigureAwait(false);

            if (members.Length > 0)
            {
                var redisKeys = members
                    .Select(x => (RedisKey)x.ToString())
                    .ToArray();

                await _database.KeyDeleteAsync(redisKeys).ConfigureAwait(false);
            }

            await _database.KeyDeleteAsync(indexKey).ConfigureAwait(false);
        }
    }
}
