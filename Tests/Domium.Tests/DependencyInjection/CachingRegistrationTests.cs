using Domium.Application.Abstractions.Query;
using Domium.Caching.Abstractions;
using Domium.Caching.Redis;
using Domium.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Domium.Tests.DependencyInjection;

public sealed class CachingRegistrationTests
{
    [Fact]
    public async Task UseCaching_serves_cacheable_query_from_cache_on_second_execution()
    {
        CountingQueryHandler.Reset();
        var services = new ServiceCollection();

        services.AddDomium(options => options.UseCaching());

        await using var provider = services.BuildServiceProvider();
        var queryBus = provider.GetRequiredService<IQueryBus>();

        var first = await queryBus.ExecuteAsync<CountingCacheableQuery, CountingResult>(new CountingCacheableQuery());
        var second = await queryBus.ExecuteAsync<CountingCacheableQuery, CountingResult>(new CountingCacheableQuery());

        Assert.Same(first, second);
        Assert.Equal(1, CountingQueryHandler.ExecutionCount);
    }

    [Fact]
    public async Task UseCaching_ignores_queries_that_do_not_opt_in()
    {
        PlainQueryHandler.Reset();
        var services = new ServiceCollection();

        services.AddDomium(options => options.UseCaching());

        await using var provider = services.BuildServiceProvider();
        var queryBus = provider.GetRequiredService<IQueryBus>();

        await queryBus.ExecuteAsync<PlainQuery, CountingResult>(new PlainQuery());
        await queryBus.ExecuteAsync<PlainQuery, CountingResult>(new PlainQuery());

        Assert.Equal(2, PlainQueryHandler.ExecutionCount);
    }

    [Fact]
    public async Task RemoveByTagAsync_invalidates_cached_query_results()
    {
        CountingQueryHandler.Reset();
        var services = new ServiceCollection();

        services.AddDomium(options => options.UseCaching());

        await using var provider = services.BuildServiceProvider();
        var queryBus = provider.GetRequiredService<IQueryBus>();
        var cache = provider.GetRequiredService<IDomiumCache>();

        await queryBus.ExecuteAsync<CountingCacheableQuery, CountingResult>(new CountingCacheableQuery());
        await cache.RemoveByTagAsync("counting");
        await queryBus.ExecuteAsync<CountingCacheableQuery, CountingResult>(new CountingCacheableQuery());

        Assert.Equal(2, CountingQueryHandler.ExecutionCount);
    }

    [Fact]
    public async Task UseCaching_uses_query_duration_when_provided()
    {
        CountingQueryHandler.Reset();
        var cache = new CapturingCache();
        var services = new ServiceCollection();

        services.AddSingleton<IDomiumCache>(cache);
        services.AddDomium(options => options.UseCaching());

        await using var provider = services.BuildServiceProvider();
        var queryBus = provider.GetRequiredService<IQueryBus>();

        await queryBus.ExecuteAsync<CountingCacheableQuery, CountingResult>(new CountingCacheableQuery());

        Assert.Equal(TimeSpan.FromMinutes(2), cache.LastOptions?.Duration);
        Assert.Contains("counting", cache.LastOptions?.Tags ?? Array.Empty<string>());
    }

    [Fact]
    public async Task UseCaching_applies_default_expiration_when_query_has_no_duration()
    {
        DefaultDurationQueryHandler.Reset();
        var cache = new CapturingCache();
        var services = new ServiceCollection();

        services.AddSingleton<IDomiumCache>(cache);
        services.AddDomium(options =>
            options.UseCaching(cacheOptions => cacheOptions.DefaultExpiration = TimeSpan.FromMinutes(7)));

        await using var provider = services.BuildServiceProvider();
        var queryBus = provider.GetRequiredService<IQueryBus>();

        await queryBus.ExecuteAsync<DefaultDurationQuery, CountingResult>(new DefaultDurationQuery());

        Assert.Equal(TimeSpan.FromMinutes(7), cache.LastOptions?.Duration);
    }

    [Fact]
    public void Redis_caching_registers_cache_store_from_options()
    {
        var services = new ServiceCollection();

        var exception = Record.Exception(() =>
            services.AddDomium(options =>
                options.UseCaching(cacheOptions => cacheOptions.Store.UseRedis("localhost"))));

        Assert.Null(exception);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IDomiumCache));
    }

    [Fact]
    public void Redis_caching_requires_explicit_connection_string()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<ArgumentException>(() =>
            services.AddDomium(options =>
                options.UseCaching(cacheOptions => cacheOptions.Store.UseRedis(string.Empty))));

        Assert.Contains("Redis connection string cannot be empty", exception.Message);
    }

    public sealed record CountingCacheableQuery : IQuery<CountingResult>, ICacheableQuery
    {
        public TimeSpan? Duration => TimeSpan.FromMinutes(2);

        public IReadOnlyCollection<string> Tags => new[] { "counting" };
    }

    public sealed record DefaultDurationQuery : IQuery<CountingResult>, ICacheableQuery;

    public sealed record PlainQuery : IQuery<CountingResult>;

    public sealed class CountingResult(int value)
    {
        public int Value { get; } = value;
    }

    public sealed class CountingQueryHandler : IQueryHandler<CountingCacheableQuery, CountingResult>
    {
        public static int ExecutionCount { get; private set; }

        public static void Reset()
        {
            ExecutionCount = 0;
        }

        public Task<CountingResult> HandleAsync(
            CountingCacheableQuery query,
            CancellationToken cancellationToken = default)
        {
            ExecutionCount++;
            return Task.FromResult(new CountingResult(ExecutionCount));
        }
    }

    public sealed class DefaultDurationQueryHandler : IQueryHandler<DefaultDurationQuery, CountingResult>
    {
        public static int ExecutionCount { get; private set; }

        public static void Reset()
        {
            ExecutionCount = 0;
        }

        public Task<CountingResult> HandleAsync(
            DefaultDurationQuery query,
            CancellationToken cancellationToken = default)
        {
            ExecutionCount++;
            return Task.FromResult(new CountingResult(ExecutionCount));
        }
    }

    public sealed class PlainQueryHandler : IQueryHandler<PlainQuery, CountingResult>
    {
        public static int ExecutionCount { get; private set; }

        public static void Reset()
        {
            ExecutionCount = 0;
        }

        public Task<CountingResult> HandleAsync(
            PlainQuery query,
            CancellationToken cancellationToken = default)
        {
            ExecutionCount++;
            return Task.FromResult(new CountingResult(ExecutionCount));
        }
    }

    private sealed class CapturingCache : IDomiumCache
    {
        public DomiumCacheEntryOptions? LastOptions { get; private set; }

        public Task<DomiumCacheResult<T>> GetAsync<T>(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(DomiumCacheResult<T>.Miss());

        public Task SetAsync<T>(string key, T value, DomiumCacheEntryOptions options, CancellationToken cancellationToken = default)
        {
            LastOptions = options;
            return Task.CompletedTask;
        }

        public Task<bool> TrySetAsync<T>(string key, T value, DomiumCacheEntryOptions options, CancellationToken cancellationToken = default)
        {
            LastOptions = options;
            return Task.FromResult(true);
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
