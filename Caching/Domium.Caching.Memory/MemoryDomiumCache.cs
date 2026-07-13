using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domium.Caching.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace Domium.Caching.Memory;

/// <summary>
/// In-memory <see cref="IDomiumCache"/> backed by <see cref="IMemoryCache"/>. A single lock
/// keeps the value store and the tag index consistent — invalidation can never miss an entry
/// that a concurrent set just wrote.
/// </summary>
public sealed class MemoryDomiumCache(IMemoryCache memoryCache) : IDomiumCache
{
    private readonly IMemoryCache _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
    private readonly Dictionary<string, HashSet<string>> _keysByTag = new(StringComparer.Ordinal);
    private readonly object _sync = new();

    public Task<DomiumCacheResult<T>> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return _memoryCache.TryGetValue(key, out var value) && value is Entry entry
                ? Task.FromResult(DomiumCacheResult<T>.Hit((T?)entry.Value))
                : Task.FromResult(DomiumCacheResult<T>.Miss());
        }
    }

    public Task SetAsync<T>(string key, T value, DomiumCacheEntryOptions options, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            Store(key, value, options);
        }

        return Task.CompletedTask;
    }

    public Task<bool> TrySetAsync<T>(string key, T value, DomiumCacheEntryOptions options, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (_memoryCache.TryGetValue(key, out _))
            {
                return Task.FromResult(false);
            }

            Store(key, value, options);
            return Task.FromResult(true);
        }
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _memoryCache.Remove(key);
        }

        return Task.CompletedTask;
    }

    public Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (!_keysByTag.TryGetValue(tag, out var keys))
            {
                return Task.CompletedTask;
            }

            foreach (var key in keys)
            {
                _memoryCache.Remove(key);
            }

            _keysByTag.Remove(tag);
        }

        return Task.CompletedTask;
    }

    private void Store<T>(string key, T value, DomiumCacheEntryOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));

        _memoryCache.Set(key, new Entry(value), options.Duration);

        foreach (var tag in options.Tags)
        {
            if (!_keysByTag.TryGetValue(tag, out var keys))
            {
                keys = new HashSet<string>(StringComparer.Ordinal);
                _keysByTag[tag] = keys;
            }

            keys.Add(key);
        }
    }

    /// <summary>Wrapper so a cached <c>null</c> is distinguishable from a miss.</summary>
    private sealed class Entry(object? value)
    {
        public object? Value { get; } = value;
    }
}
