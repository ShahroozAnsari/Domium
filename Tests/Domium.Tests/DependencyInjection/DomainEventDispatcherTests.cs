using Domium.Application.Abstractions.Command;
using Domium.Domain;
using Domium.Domain.Abstractions.Events;
using Domium.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Domium.Tests.DependencyInjection;

public sealed class DomainEventDispatcherTests
{
    [Fact]
    public async Task AddDomium_registers_domain_event_handlers_and_dispatcher()
    {
        PingedHandler.Reset();
        var services = new ServiceCollection();

        services.AddDomium();

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IDomainEventDispatcher>();

        await dispatcher.DispatchAsync(new IDomainEvent[] { new PingedDomainEvent("ready") });

        Assert.Equal("ready", PingedHandler.LastMessage);
    }

    [Fact]
    public void AddDomium_allows_explicitly_disabled_transactions_without_unit_of_work()
    {
        var services = new ServiceCollection();

        var exception = Record.Exception(() =>
            services.AddDomium(options => options.UseTransactions(false)));

        Assert.Null(exception);
    }

    [Fact]
    public void AddDomium_fails_when_multiple_command_handlers_are_registered_for_same_command()
    {
        var services = new ServiceCollection();

        services.AddScoped<ICommandHandler<DuplicateCommand>, DuplicateCommandHandler>();
        services.AddScoped<ICommandHandler<DuplicateCommand>, AnotherDuplicateCommandHandler>();

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddDomium(options => options.UseLoadedAssemblyScanning(false)));

        Assert.Contains("multiple command handlers", exception.Message);
    }

    public sealed class PingedDomainEvent(string message) : DomainEvent
    {
        public string Message { get; } = message;
    }

    public sealed class PingedHandler : IDomainEventHandler<PingedDomainEvent>
    {
        public static string? LastMessage { get; private set; }

        public static void Reset()
        {
            LastMessage = null;
        }

        public Task HandleAsync(
            PingedDomainEvent domainEvent,
            CancellationToken cancellationToken = default)
        {
            LastMessage = domainEvent.Message;
            return Task.CompletedTask;
        }
    }

    public sealed class DuplicateCommand : ICommand
    {
    }

    private abstract class DuplicateCommandHandler : ICommandHandler<DuplicateCommand>
    {
        public Task HandleAsync(DuplicateCommand command, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private abstract class AnotherDuplicateCommandHandler : ICommandHandler<DuplicateCommand>
    {
        public Task HandleAsync(DuplicateCommand command, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
