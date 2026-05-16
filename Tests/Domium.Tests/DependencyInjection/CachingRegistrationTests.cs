using Domium.Application.Abstractions.Query;
using Domium.Caching.Abstractions.Models;
using Domium.Caching.Abstractions.Providers;
using Domium.Caching.Abstractions.Results;
using Domium.Caching.Abstractions.Stores;
using Domium.Caching.Exceptions;
using Domium.Caching.Providers;
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

        services.AddSingleton<IDomiumCacheStore>(cacheStore);
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

    private static void RegisterPolicy(
        IServiceProvider provider,
        DomiumQueryCacheScopeMode scopeMode,
        TimeSpan? absoluteExpirationRelativeToNow = null,
        TimeSpan? slidingExpiration = null)
    {
        var policyProvider = Assert.IsType<DomiumQueryCachePolicyProvider>(
            provider.GetRequiredService<IDomiumQueryCachePolicyProvider>());

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

    public sealed class CountingResult
    {
        public CountingResult(int value)
        {
            Value = value;
        }

        public int Value { get; }
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

    private sealed class CapturingCacheStore : IDomiumCacheStore
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
