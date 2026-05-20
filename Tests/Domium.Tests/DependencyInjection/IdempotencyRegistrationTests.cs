using Domium.Application.Abstractions.Command;
using Domium.Caching.Abstractions.Stores;
using Domium.Configuration;
using Domium.Extensions.DependencyInjection;
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
    public void Redis_idempotency_uses_own_cache_store_configuration()
    {
        var services = new ServiceCollection();

        var exception = Record.Exception(() =>
            services.AddDomium(options =>
                options
                    .UseIdempotency(idempotency =>
                    {
                        idempotency.Store.Provider = DomiumCacheProvider.Redis;
                        idempotency.Store.RedisConnectionString = "localhost";
                    })));

        Assert.Null(exception);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IDomiumIdempotencyCacheStore));
    }

    [Fact]
    public void Redis_idempotency_store_configuration_requires_connection_string()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddDomium(options =>
                options
                    .UseIdempotency(idempotency =>
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
            () => provider.GetRequiredService<IDomiumIdempotencyCacheStore>());
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
}
