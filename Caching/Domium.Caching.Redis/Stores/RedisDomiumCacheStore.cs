using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Domium.Caching.Abstractions.Models;
using Domium.Caching.Abstractions.Results;
using Domium.Caching.Abstractions.Stores;
using Domium.Caching.Redis.Internal;
using StackExchange.Redis;

namespace Domium.Caching.Redis.Stores;

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

        var now = DateTimeOffset.UtcNow;

        if (envelope.AbsoluteExpiresAtUtc.HasValue && envelope.AbsoluteExpiresAtUtc.Value <= now)
        {
            await RemoveAsync(key, cancellationToken).ConfigureAwait(false);
            return DomiumCacheResult<T>.Miss();
        }

        if (envelope.SlidingExpiration.HasValue)
        {
            var ttl = envelope.SlidingExpiration.Value;

            if (envelope.AbsoluteExpiresAtUtc.HasValue)
            {
                var remainingAbsoluteTtl = envelope.AbsoluteExpiresAtUtc.Value - now;

                if (remainingAbsoluteTtl <= TimeSpan.Zero)
                {
                    await RemoveAsync(key, cancellationToken).ConfigureAwait(false);
                    return DomiumCacheResult<T>.Miss();
                }

                if (remainingAbsoluteTtl < ttl)
                {
                    ttl = remainingAbsoluteTtl;
                }
            }

            await _database.KeyExpireAsync(key, ttl).ConfigureAwait(false);
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
        var payload = CreatePayload(value, options, metadata);
        var ttl = GetInitialTtl(options);

        await _database.StringSetAsync(
            key,
            payload,
            expiry: ttl.HasValue ? (Expiration)ttl.Value : default).ConfigureAwait(false);

        await AddIndexesAsync(key, metadata).ConfigureAwait(false);
    }

    public async Task<bool> TrySetAsync<T>(
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
        var payload = CreatePayload(value, options, metadata);
        var ttl = GetInitialTtl(options);

        var stored = await _database.StringSetAsync(
            key,
            payload,
            expiry: ttl.HasValue ? (Expiration)ttl.Value : default,
            when: When.NotExists).ConfigureAwait(false);

        if (stored)
        {
            await AddIndexesAsync(key, metadata).ConfigureAwait(false);
        }

        return stored;
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

        var metadata = await GetMetadataAsync(key).ConfigureAwait(false);
        await _database.KeyDeleteAsync(key).ConfigureAwait(false);

        if (metadata is not null)
        {
            await RemoveIndexesAsync(key, metadata).ConfigureAwait(false);
        }
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
            var entries = new List<(string Key, DomiumCacheInvalidationMetadata? Metadata)>(members.Length);

            foreach (var member in members)
            {
                var key = member.ToString();
                entries.Add((key, await GetMetadataAsync(key).ConfigureAwait(false)));
            }

            var redisKeys = members
                .Select(x => (RedisKey)x.ToString())
                .ToArray();

            await _database.KeyDeleteAsync(redisKeys).ConfigureAwait(false);

            foreach (var entry in entries)
            {
                if (entry.Metadata is not null)
                {
                    await RemoveIndexesAsync(entry.Key, entry.Metadata).ConfigureAwait(false);
                }
            }
        }

        await _database.KeyDeleteAsync(indexKey).ConfigureAwait(false);
    }

    private static TimeSpan? GetInitialTtl(DomiumCacheEntryOptions? options)
    {
        if (options is null)
        {
            return null;
        }

        if (options.AbsoluteExpirationRelativeToNow.HasValue &&
            options.SlidingExpiration.HasValue)
        {
            return options.AbsoluteExpirationRelativeToNow.Value <= options.SlidingExpiration.Value
                ? options.AbsoluteExpirationRelativeToNow.Value
                : options.SlidingExpiration.Value;
        }

        return options.AbsoluteExpirationRelativeToNow ?? options.SlidingExpiration;
    }

    private static string CreatePayload<T>(
        T value,
        DomiumCacheEntryOptions? options,
        DomiumCacheInvalidationMetadata metadata)
    {
        var now = DateTimeOffset.UtcNow;
        var absoluteExpiresAtUtc = options?.AbsoluteExpirationRelativeToNow.HasValue == true
            ? now.Add(options.AbsoluteExpirationRelativeToNow.Value)
            : (DateTimeOffset?)null;

        var envelope = new RedisDomiumCacheEnvelope<T>
        {
            HasValue = true,
            Value = value,
            Metadata = metadata,
            AbsoluteExpiresAtUtc = absoluteExpiresAtUtc,
            SlidingExpiration = options?.SlidingExpiration
        };

        return JsonSerializer.Serialize(envelope, SerializerOptions);
    }

    private async Task<DomiumCacheInvalidationMetadata?> GetMetadataAsync(string key)
    {
        var redisValue = await _database.StringGetAsync(key).ConfigureAwait(false);

        if (!redisValue.HasValue)
        {
            return null;
        }

        var payload = redisValue.ToString();

        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        var envelope = JsonSerializer.Deserialize<RedisDomiumCacheMetadataEnvelope>(
            payload,
            SerializerOptions);

        return envelope?.Metadata;
    }

    private async Task RemoveIndexesAsync(
        string key,
        DomiumCacheInvalidationMetadata metadata)
    {
        foreach (var tag in metadata.Tags.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            await RemoveIndexMemberAsync(RedisInvalidationIndexKeys.Tag(tag.Trim()), key).ConfigureAwait(false);
        }

        foreach (var entityKey in metadata.EntityKeys.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            await RemoveIndexMemberAsync(RedisInvalidationIndexKeys.EntityKey(entityKey.Trim()), key).ConfigureAwait(false);
        }

        if (!string.IsNullOrWhiteSpace(metadata.Group))
        {
            await RemoveIndexMemberAsync(RedisInvalidationIndexKeys.Group(metadata.Group.Trim()), key).ConfigureAwait(false);
        }
    }

    private async Task RemoveIndexMemberAsync(string indexKey, string key)
    {
        await _database.SetRemoveAsync(indexKey, key).ConfigureAwait(false);

        if (await _database.SetLengthAsync(indexKey).ConfigureAwait(false) == 0)
        {
            await _database.KeyDeleteAsync(indexKey).ConfigureAwait(false);
        }
    }

    private sealed class RedisDomiumCacheMetadataEnvelope
    {
        public DomiumCacheInvalidationMetadata? Metadata { get; set; }
    }
}
