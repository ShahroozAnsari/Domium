using Domium.Caching.Abstractions.Models;
using Domium.Caching.Memory.Stores;
using Microsoft.Extensions.Caching.Memory;

namespace Domium.Tests.Caching;

public sealed class MemoryDomiumCacheStoreTests
{
    [Fact]
    public async Task TrySetAsync_stores_value_only_when_key_is_absent()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var store = new MemoryDomiumCacheStore(memoryCache);
        var options = new DomiumCacheEntryOptions(TimeSpan.FromMinutes(5), null);
        var metadata = new DomiumCacheInvalidationMetadata(null, null, null);

        var firstStored = await store.TrySetAsync("key", "first", options, metadata, CancellationToken.None);
        var secondStored = await store.TrySetAsync("key", "second", options, metadata, CancellationToken.None);
        var cached = await store.TryGetAsync<string>("key", CancellationToken.None);

        Assert.True(firstStored);
        Assert.False(secondStored);
        Assert.True(cached.HasValue);
        Assert.Equal("first", cached.Value);
    }

    [Fact]
    public async Task TrySetAsync_allows_same_key_after_remove()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var store = new MemoryDomiumCacheStore(memoryCache);
        var options = new DomiumCacheEntryOptions(TimeSpan.FromMinutes(5), null);
        var metadata = new DomiumCacheInvalidationMetadata(null, null, null);

        await store.TrySetAsync("key", "first", options, metadata, CancellationToken.None);
        await store.RemoveAsync("key", CancellationToken.None);
        var storedAgain = await store.TrySetAsync("key", "second", options, metadata, CancellationToken.None);
        var cached = await store.TryGetAsync<string>("key", CancellationToken.None);

        Assert.True(storedAgain);
        Assert.True(cached.HasValue);
        Assert.Equal("second", cached.Value);
    }

    [Fact]
    public async Task TrySetAsync_allows_same_key_after_expiration()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var store = new MemoryDomiumCacheStore(memoryCache);
        var options = new DomiumCacheEntryOptions(TimeSpan.FromMilliseconds(20), null);
        var metadata = new DomiumCacheInvalidationMetadata(null, null, null);

        await store.TrySetAsync("key", "first", options, metadata, CancellationToken.None);
        await Task.Delay(TimeSpan.FromMilliseconds(80));
        var storedAgain = await store.TrySetAsync("key", "second", options, metadata, CancellationToken.None);
        var cached = await store.TryGetAsync<string>("key", CancellationToken.None);

        Assert.True(storedAgain);
        Assert.True(cached.HasValue);
        Assert.Equal("second", cached.Value);
    }

    [Fact]
    public async Task RemoveByGroupAsync_removes_entries_created_by_try_set()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var store = new MemoryDomiumCacheStore(memoryCache);
        var options = new DomiumCacheEntryOptions(TimeSpan.FromMinutes(5), null);
        var metadata = new DomiumCacheInvalidationMetadata(null, null, "orders");

        await store.TrySetAsync("key", "value", options, metadata, CancellationToken.None);
        await store.RemoveByGroupAsync("orders", CancellationToken.None);
        var cached = await store.TryGetAsync<string>("key", CancellationToken.None);

        Assert.False(cached.HasValue);
    }

    [Fact]
    public async Task TrySetAsync_throws_for_empty_key()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var store = new MemoryDomiumCacheStore(memoryCache);
        var options = new DomiumCacheEntryOptions(TimeSpan.FromMinutes(5), null);
        var metadata = new DomiumCacheInvalidationMetadata(null, null, null);

        await Assert.ThrowsAsync<ArgumentException>(
            () => store.TrySetAsync(" ", "value", options, metadata, CancellationToken.None));
    }
}
