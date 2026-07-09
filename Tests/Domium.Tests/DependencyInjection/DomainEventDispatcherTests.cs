using Domium.Application.Abstractions.Events;
using Domium.Application.Abstractions.Command;
using Domium.Domain;
using Domium.Eventing.Abstractions;
using Domium.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Domium.Tests.DependencyInjection;

public sealed class DomainEventBusRegistrationTests
{
    [Fact]
    public async Task AddDomium_registers_domain_event_handlers_and_event_bus()
    {
        PingedHandler.Reset();
        var services = new ServiceCollection();

        services.AddDomium();

        await using var provider = services.BuildServiceProvider();
        var eventBus = provider.GetRequiredService<IEventBus>();

        await eventBus.PublishAsync(new PingedDomainEvent("ready"));

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
        Assert.Contains(nameof(DuplicateCommandHandler), exception.Message);
        Assert.Contains(nameof(AnotherDuplicateCommandHandler), exception.Message);
    }

    [Fact]
    public void AddDomium_respects_loaded_assembly_name_prefix_filter()
    {
        var services = new ServiceCollection();

        services.AddDomium(options =>
            options.AddApplicationAssemblyNamePrefix("Definitely.Not.Domium.Tests"));

        using var provider = services.BuildServiceProvider();

        Assert.Empty(provider.GetServices<IDomainEventHandler<PingedDomainEvent>>());
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
