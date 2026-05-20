using Domium.Application.Abstractions.Query;
using Domium.Caching.Abstractions.Models;
using Domium.Caching.Abstractions.Providers;
using Domium.Caching.Abstractions.Results;
using Domium.Caching.Abstractions.Stores;
using Domium.Caching.Exceptions;
using Domium.Caching.Providers;
using Domium.Configuration;
using Domium.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Domium.Tests.DependencyInjection;

public sealed class CachingRegistrationTests
{
    [Fact]
    public async Task UseCaching_resolves_query_pipeline_and_reuses_cached_global_result()
    {
        CountingQueryHandler.Reset();
        var services = new ServiceCollection();

        services.AddDomium(options => options.UseCaching());

        await using var provider = services.BuildServiceProvider();
        RegisterPolicy(provider, DomiumQueryCacheScopeMode.Global);

        var queryBus = provider.GetRequiredService<IQueryBus>();

        var first = await queryBus.ExecuteAsync<CountingQuery, CountingResult>(new CountingQuery());
        var second = await queryBus.ExecuteAsync<CountingQuery, CountingResult>(new CountingQuery());

        Assert.Same(first, second);
        Assert.Equal(1, CountingQueryHandler.ExecutionCount);
    }

    [Fact]
    public async Task UseCaching_preserves_policy_entry_options_when_storing_result()
    {
        var cacheStore = new CapturingCacheStore();
        var services = new ServiceCollection();

        services.AddSingleton<IDomiumQueryCacheStore>(cacheStore);
        services.AddDomium(options => options.UseCaching());

        await using var provider = services.BuildServiceProvider();
        RegisterPolicy(
            provider,
            DomiumQueryCacheScopeMode.Global,
            absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(2),
            slidingExpiration: TimeSpan.FromSeconds(30));

        var queryBus = provider.GetRequiredService<IQueryBus>();

        await queryBus.ExecuteAsync<CountingQuery, CountingResult>(new CountingQuery());

        Assert.Equal(TimeSpan.FromMinutes(2), cacheStore.EntryOptions?.AbsoluteExpirationRelativeToNow);
        Assert.Equal(TimeSpan.FromSeconds(30), cacheStore.EntryOptions?.SlidingExpiration);
    }

    [Fact]
    public async Task UseCaching_applies_default_expiration_when_policy_has_no_expiration()
    {
        var cacheStore = new CapturingCacheStore();
        var services = new ServiceCollection();

        services.AddSingleton<IDomiumQueryCacheStore>(cacheStore);
        services.AddDomium(options =>
            options.UseCaching(cacheOptions =>
                cacheOptions.DefaultExpiration = TimeSpan.FromMinutes(7)));

        await using var provider = services.BuildServiceProvider();
        RegisterPolicy(provider, DomiumQueryCacheScopeMode.Global);

        var queryBus = provider.GetRequiredService<IQueryBus>();

        await queryBus.ExecuteAsync<CountingQuery, CountingResult>(new CountingQuery());

        Assert.Equal(TimeSpan.FromMinutes(7), cacheStore.EntryOptions?.AbsoluteExpirationRelativeToNow);
    }

    [Fact]
    public async Task Tenant_scoped_cache_policy_fails_clearly_without_tenant_context()
    {
        var services = new ServiceCollection();

        services.AddDomium(options => options.UseCaching());

        await using var provider = services.BuildServiceProvider();
        RegisterPolicy(provider, DomiumQueryCacheScopeMode.Tenant);

        var queryBus = provider.GetRequiredService<IQueryBus>();

        await Assert.ThrowsAsync<DomiumCacheScopeResolutionException>(
            () => queryBus.ExecuteAsync<CountingQuery, CountingResult>(new CountingQuery()));
    }

    [Fact]
    public void Redis_caching_registers_cache_store_from_options()
    {
        var services = new ServiceCollection();

        var exception = Record.Exception(() =>
            services.AddDomium(options =>
                options.UseCaching(cacheOptions =>
                {
                    cacheOptions.Store.Provider = DomiumCacheProvider.Redis;
                    cacheOptions.Store.RedisConnectionString = "localhost";
                })));

        Assert.Null(exception);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IDomiumQueryCacheStore));
    }

    [Fact]
    public void Redis_caching_requires_explicit_connection_string()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddDomium(options =>
                options.UseCaching(cacheOptions =>
                {
                    cacheOptions.Store.Provider = DomiumCacheProvider.Redis;
                    cacheOptions.Store.RedisConnectionString = string.Empty;
                })));

        Assert.Contains("Query caching Redis store requires a non-empty Redis connection string", exception.Message);
    }

    private static void RegisterPolicy(
        IServiceProvider provider,
        DomiumQueryCacheScopeMode scopeMode,
        TimeSpan? absoluteExpirationRelativeToNow = null,
        TimeSpan? slidingExpiration = null)
    {
        var policyProvider = provider.GetRequiredService<IDomiumQueryCachePolicyRegistry>();

        policyProvider.Register(
            new DomiumQueryCachePolicy(
                typeof(CountingQuery),
                enabled: true,
                scopeMode,
                absoluteExpirationRelativeToNow,
                slidingExpiration,
                keyPrefix: null,
                cacheNullValues: false,
                invalidationMetadata: null));
    }

    public sealed class CountingQuery : IQuery<CountingResult>
    {
    }

    public sealed class CountingResult(int value)
    {
        public int Value { get; } = value;
    }

    public sealed class CountingQueryHandler : IQueryHandler<CountingQuery, CountingResult>
    {
        public static int ExecutionCount { get; private set; }

        public static void Reset()
        {
            ExecutionCount = 0;
        }

        public Task<CountingResult> HandleAsync(
            CountingQuery query,
            CancellationToken cancellationToken = default)
        {
            ExecutionCount++;
            return Task.FromResult(new CountingResult(ExecutionCount));
        }
    }

    private sealed class CapturingCacheStore : IDomiumQueryCacheStore
    {
        public DomiumCacheEntryOptions? EntryOptions { get; private set; }

        public Task<DomiumCacheResult<T>> TryGetAsync<T>(
            string key,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(DomiumCacheResult<T>.Miss());
        }

        public Task SetAsync<T>(
            string key,
            T value,
            DomiumCacheEntryOptions options,
            DomiumCacheInvalidationMetadata invalidationMetadata,
            CancellationToken cancellationToken)
        {
            EntryOptions = options;
            return Task.CompletedTask;
        }

        public Task<bool> TrySetAsync<T>(
            string key,
            T value,
            DomiumCacheEntryOptions options,
            DomiumCacheInvalidationMetadata invalidationMetadata,
            CancellationToken cancellationToken)
        {
            EntryOptions = options;
            return Task.FromResult(true);
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task RemoveByTagAsync(string tag, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task RemoveByEntityKeyAsync(string entityKey, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task RemoveByGroupAsync(string group, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
