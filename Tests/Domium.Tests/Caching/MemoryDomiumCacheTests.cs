using Domium.Caching.Abstractions;
using Domium.Caching.Memory;
using Microsoft.Extensions.Caching.Memory;

namespace Domium.Tests.Caching;

public sealed class MemoryDomiumCacheTests
{
    [Fact]
    public async Task TrySetAsync_stores_value_only_when_key_is_absent()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new MemoryDomiumCache(memoryCache);
        var options = new DomiumCacheEntryOptions(TimeSpan.FromMinutes(5));

        var firstStored = await cache.TrySetAsync("key", "first", options);
        var secondStored = await cache.TrySetAsync("key", "second", options);
        var cached = await cache.GetAsync<string>("key");

        Assert.True(firstStored);
        Assert.False(secondStored);
        Assert.True(cached.Found);
        Assert.Equal("first", cached.Value);
    }

    [Fact]
    public async Task TrySetAsync_allows_same_key_after_remove()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new MemoryDomiumCache(memoryCache);
        var options = new DomiumCacheEntryOptions(TimeSpan.FromMinutes(5));

        await cache.TrySetAsync("key", "first", options);
        await cache.RemoveAsync("key");
        var storedAgain = await cache.TrySetAsync("key", "second", options);
        var cached = await cache.GetAsync<string>("key");

        Assert.True(storedAgain);
        Assert.True(cached.Found);
        Assert.Equal("second", cached.Value);
    }

    [Fact]
    public async Task TrySetAsync_allows_same_key_after_expiration()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new MemoryDomiumCache(memoryCache);
        var options = new DomiumCacheEntryOptions(TimeSpan.FromMilliseconds(20));

        await cache.TrySetAsync("key", "first", options);
        await Task.Delay(TimeSpan.FromMilliseconds(80));
        var storedAgain = await cache.TrySetAsync("key", "second", options);
        var cached = await cache.GetAsync<string>("key");

        Assert.True(storedAgain);
        Assert.True(cached.Found);
        Assert.Equal("second", cached.Value);
    }

    [Fact]
    public async Task RemoveByTagAsync_removes_every_entry_stored_with_the_tag()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new MemoryDomiumCache(memoryCache);
        var tagged = new DomiumCacheEntryOptions(TimeSpan.FromMinutes(5), new[] { "orders" });
        var untagged = new DomiumCacheEntryOptions(TimeSpan.FromMinutes(5));

        await cache.SetAsync("orders:1", "a", tagged);
        await cache.SetAsync("orders:2", "b", tagged);
        await cache.SetAsync("customers:1", "c", untagged);

        await cache.RemoveByTagAsync("orders");

        Assert.False((await cache.GetAsync<string>("orders:1")).Found);
        Assert.False((await cache.GetAsync<string>("orders:2")).Found);
        Assert.True((await cache.GetAsync<string>("customers:1")).Found);
    }

    [Fact]
    public async Task GetAsync_distinguishes_cached_null_from_miss()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new MemoryDomiumCache(memoryCache);
        var options = new DomiumCacheEntryOptions(TimeSpan.FromMinutes(5));

        await cache.SetAsync<string?>("nullable", null, options);

        var hit = await cache.GetAsync<string?>("nullable");
        var miss = await cache.GetAsync<string?>("absent");

        Assert.True(hit.Found);
        Assert.Null(hit.Value);
        Assert.False(miss.Found);
    }

    [Fact]
    public void EntryOptions_reject_non_positive_duration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DomiumCacheEntryOptions(TimeSpan.Zero));
    }
}
