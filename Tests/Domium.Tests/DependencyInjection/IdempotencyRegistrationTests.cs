using Domium.Application.Abstractions.Command;
using Domium.Caching.Abstractions;
using Domium.Configuration;
using Domium.Extensions.DependencyInjection;
using Domium.Idempotency.Abstractions.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Domium.Tests.DependencyInjection;

public sealed class IdempotencyRegistrationTests
{
    [Fact]
    public async Task UseIdempotency_executes_idempotent_command_once_for_same_key()
    {
        CountingIdempotentCommandHandler.Reset();
        var services = new ServiceCollection();

        services.AddDomium(options => options.UseIdempotency());

        await using var provider = services.BuildServiceProvider();
        var commandBus = provider.GetRequiredService<ICommandBus>();
        var command = new CountingIdempotentCommand("same-key");

        await commandBus.ExecuteAsync(command);
        await commandBus.ExecuteAsync(command);

        Assert.Equal(1, CountingIdempotentCommandHandler.ExecutionCount);
    }

    [Fact]
    public async Task UseIdempotency_executes_idempotent_command_for_different_keys()
    {
        CountingIdempotentCommandHandler.Reset();
        var services = new ServiceCollection();

        services.AddDomium(options => options.UseIdempotency());

        await using var provider = services.BuildServiceProvider();
        var commandBus = provider.GetRequiredService<ICommandBus>();

        await commandBus.ExecuteAsync(new CountingIdempotentCommand("first"));
        await commandBus.ExecuteAsync(new CountingIdempotentCommand("second"));

        Assert.Equal(2, CountingIdempotentCommandHandler.ExecutionCount);
    }

    [Fact]
    public async Task UseIdempotency_removes_reservation_when_command_fails()
    {
        FailingOnceIdempotentCommandHandler.Reset();
        var services = new ServiceCollection();

        services.AddDomium(options => options.UseIdempotency());

        await using var provider = services.BuildServiceProvider();
        var commandBus = provider.GetRequiredService<ICommandBus>();
        var command = new FailingOnceIdempotentCommand("retry-key");

        await Assert.ThrowsAsync<InvalidOperationException>(() => commandBus.ExecuteAsync(command));
        await commandBus.ExecuteAsync(command);

        Assert.Equal(2, FailingOnceIdempotentCommandHandler.ExecutionCount);
    }

    [Fact]
    public async Task UseIdempotency_does_not_remove_reservation_when_completion_mark_fails()
    {
        CountingIdempotentCommandHandler.Reset();
        var cache = new CompletionFailingCache();
        var services = new ServiceCollection();

        services.AddSingleton<IDomiumCache>(cache);
        services.AddDomium(options => options.UseIdempotency());

        await using var provider = services.BuildServiceProvider();
        var commandBus = provider.GetRequiredService<ICommandBus>();
        var command = new CountingIdempotentCommand("completion-fails");

        await Assert.ThrowsAsync<TestCompletionException>(() => commandBus.ExecuteAsync(command));
        await commandBus.ExecuteAsync(command);

        Assert.Equal(1, CountingIdempotentCommandHandler.ExecutionCount);
        Assert.Equal(0, cache.RemoveCount);
    }

    [Fact]
    public async Task UseIdempotency_skips_handler_when_duplicate_outcome_is_unknown()
    {
        CountingIdempotentCommandHandler.Reset();
        var services = new ServiceCollection();

        services.AddSingleton<IDomiumCache>(new AlreadyReservedCache(existingEntry: null));
        services.AddDomium(options => options.UseIdempotency());

        await using var provider = services.BuildServiceProvider();
        var commandBus = provider.GetRequiredService<ICommandBus>();

        await commandBus.ExecuteAsync(new CountingIdempotentCommand("reserved"));

        Assert.Equal(0, CountingIdempotentCommandHandler.ExecutionCount);
    }

    [Fact]
    public async Task UseIdempotency_throws_when_duplicate_is_still_in_progress()
    {
        CountingIdempotentCommandHandler.Reset();
        var inProgress = new DomiumIdempotencyEntry(
            "key",
            "command",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddHours(1));
        var services = new ServiceCollection();

        services.AddSingleton<IDomiumCache>(new AlreadyReservedCache(inProgress));
        services.AddDomium(options => options.UseIdempotency());

        await using var provider = services.BuildServiceProvider();
        var commandBus = provider.GetRequiredService<ICommandBus>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => commandBus.ExecuteAsync(new CountingIdempotentCommand("reserved")));

        Assert.Contains("in progress", exception.Message);
        Assert.Equal(0, CountingIdempotentCommandHandler.ExecutionCount);
    }

    [Fact]
    public async Task UseIdempotency_rejects_empty_idempotency_key()
    {
        EmptyKeyCommandHandler.Reset();
        var services = new ServiceCollection();

        services.AddDomium(options => options.UseIdempotency());

        await using var provider = services.BuildServiceProvider();
        var commandBus = provider.GetRequiredService<ICommandBus>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => commandBus.ExecuteAsync(new EmptyKeyCommand()));

        Assert.Contains("non-empty idempotency key", exception.Message);
        Assert.Equal(0, EmptyKeyCommandHandler.ExecutionCount);
    }

    [Fact]
    public async Task UseIdempotency_can_require_all_commands_to_have_idempotency_key()
    {
        var services = new ServiceCollection();

        services.AddDomium(options =>
            options.UseIdempotency(idempotency =>
                idempotency.RequireIdempotencyKey = true));

        await using var provider = services.BuildServiceProvider();
        var commandBus = provider.GetRequiredService<ICommandBus>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => commandBus.ExecuteAsync(new NonIdempotentCommand()));

        Assert.Contains(nameof(IIdempotentCommand), exception.Message);
    }

    [Fact]
    public void UseIdempotency_requires_positive_expiration()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddDomium(options =>
                options.UseIdempotency(idempotency =>
                    idempotency.Expiration = TimeSpan.Zero)));

        Assert.Contains("Idempotency expiration must be greater than zero", exception.Message);
    }

    [Fact]
    public void UseIdempotency_requires_key_prefix()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddDomium(options =>
                options.UseIdempotency(idempotency =>
                    idempotency.KeyPrefix = " ")));

        Assert.Contains("Idempotency key prefix cannot be empty", exception.Message);
    }

    [Fact]
    public async Task UseIdempotency_skips_non_idempotent_commands_by_default()
    {
        NonIdempotentCommandHandler.Reset();
        var services = new ServiceCollection();

        services.AddDomium(options => options.UseIdempotency());

        await using var provider = services.BuildServiceProvider();
        var commandBus = provider.GetRequiredService<ICommandBus>();

        await commandBus.ExecuteAsync(new NonIdempotentCommand());
        await commandBus.ExecuteAsync(new NonIdempotentCommand());

        Assert.Equal(2, NonIdempotentCommandHandler.ExecutionCount);
    }

    [Fact]
    public void Redis_idempotency_registers_cache_store_from_options()
    {
        var services = new ServiceCollection();

        var exception = Record.Exception(() =>
            services.AddDomium(options =>
                options.UseIdempotency(idempotency =>
                {
                    idempotency.Store.Provider = DomiumCacheProvider.Redis;
                    idempotency.Store.RedisConnectionString = "localhost";
                })));

        Assert.Null(exception);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IDomiumCache));
    }

    [Fact]
    public void Redis_idempotency_store_configuration_requires_connection_string()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddDomium(options =>
                options.UseIdempotency(idempotency =>
                {
                    idempotency.Store.Provider = DomiumCacheProvider.Redis;
                    idempotency.Store.RedisConnectionString = string.Empty;
                })));

        Assert.Contains("Idempotency Redis store requires a non-empty Redis connection string", exception.Message);
    }

    [Fact]
    public void Redis_idempotency_uses_connection_factory_when_resolving_store()
    {
        var services = new ServiceCollection();

        services.AddDomium(options =>
            options.UseIdempotency(idempotency =>
                idempotency.Store.UseRedis(_ => throw new TestConnectionFactoryException())));

        using var provider = services.BuildServiceProvider();

        Assert.Throws<TestConnectionFactoryException>(
            () => provider.GetRequiredService<IDomiumCache>());
    }

    [Fact]
    public void Query_caching_and_idempotency_share_one_cache_store()
    {
        var services = new ServiceCollection();

        services.AddDomium(options =>
        {
            options.UseCaching(cache => cache.Store.UseMemory());
            options.UseIdempotency(idempotency => idempotency.Store.UseMemory());
        });

        using var provider = services.BuildServiceProvider();

        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IDomiumCache));
        Assert.NotNull(provider.GetRequiredService<IDomiumCache>());
    }

    public sealed class CountingIdempotentCommand(string idempotencyKey) : IIdempotentCommand
    {
        public string IdempotencyKey { get; } = idempotencyKey;
    }

    public sealed class CountingIdempotentCommandHandler : ICommandHandler<CountingIdempotentCommand>
    {
        public static int ExecutionCount { get; private set; }

        public static void Reset()
        {
            ExecutionCount = 0;
        }

        public Task HandleAsync(
            CountingIdempotentCommand command,
            CancellationToken cancellationToken = default)
        {
            ExecutionCount++;
            return Task.CompletedTask;
        }
    }

    public sealed class FailingOnceIdempotentCommand(string idempotencyKey) : IIdempotentCommand
    {
        public string IdempotencyKey { get; } = idempotencyKey;
    }

    public sealed class FailingOnceIdempotentCommandHandler : ICommandHandler<FailingOnceIdempotentCommand>
    {
        public static int ExecutionCount { get; private set; }

        public static void Reset()
        {
            ExecutionCount = 0;
        }

        public Task HandleAsync(
            FailingOnceIdempotentCommand command,
            CancellationToken cancellationToken = default)
        {
            ExecutionCount++;

            if (ExecutionCount == 1)
            {
                throw new InvalidOperationException("First execution fails.");
            }

            return Task.CompletedTask;
        }
    }

    public sealed class EmptyKeyCommand : IIdempotentCommand
    {
        public string IdempotencyKey => " ";
    }

    public sealed class EmptyKeyCommandHandler : ICommandHandler<EmptyKeyCommand>
    {
        public static int ExecutionCount { get; private set; }

        public static void Reset()
        {
            ExecutionCount = 0;
        }

        public Task HandleAsync(
            EmptyKeyCommand command,
            CancellationToken cancellationToken = default)
        {
            ExecutionCount++;
            return Task.CompletedTask;
        }
    }

    public sealed class NonIdempotentCommand : ICommand
    {
    }

    public sealed class NonIdempotentCommandHandler : ICommandHandler<NonIdempotentCommand>
    {
        public static int ExecutionCount { get; private set; }

        public static void Reset()
        {
            ExecutionCount = 0;
        }

        public Task HandleAsync(
            NonIdempotentCommand command,
            CancellationToken cancellationToken = default)
        {
            ExecutionCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class TestConnectionFactoryException : Exception
    {
    }

    private sealed class TestCompletionException : Exception
    {
    }

    /// <summary>TrySet always fails; Get returns the given entry (or a miss when null).</summary>
    private sealed class AlreadyReservedCache(DomiumIdempotencyEntry? existingEntry) : IDomiumCache
    {
        public Task<DomiumCacheResult<T>> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            if (existingEntry is T typed)
            {
                return Task.FromResult(DomiumCacheResult<T>.Hit(typed));
            }

            return Task.FromResult(DomiumCacheResult<T>.Miss());
        }

        public Task SetAsync<T>(string key, T value, DomiumCacheEntryOptions options, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> TrySetAsync<T>(string key, T value, DomiumCacheEntryOptions options, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>Reservations succeed; marking completion (SetAsync) always fails.</summary>
    private sealed class CompletionFailingCache : IDomiumCache
    {
        private readonly HashSet<string> _reservedKeys = new(StringComparer.Ordinal);

        public int RemoveCount { get; private set; }

        public Task<DomiumCacheResult<T>> GetAsync<T>(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(DomiumCacheResult<T>.Miss());

        public Task SetAsync<T>(string key, T value, DomiumCacheEntryOptions options, CancellationToken cancellationToken = default) =>
            throw new TestCompletionException();

        public Task<bool> TrySetAsync<T>(string key, T value, DomiumCacheEntryOptions options, CancellationToken cancellationToken = default) =>
            Task.FromResult(_reservedKeys.Add(key));

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            RemoveCount++;
            _reservedKeys.Remove(key);
            return Task.CompletedTask;
        }

        public Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
