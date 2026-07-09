using Domium.Application.Abstractions.Events;
using Domium.Domain;
using Domium.Eventing.Abstractions;
using Domium.Eventing.Abstractions.External;
using Domium.Eventing.Abstractions.Internal;
using Domium.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Domium.Tests.Eventing;

public sealed class EventingTests
{
    [Fact]
    public async Task AddDomium_registers_internal_event_handlers_and_noop_external_publisher()
    {
        InternalPingHandler.Reset();
        var services = new ServiceCollection();

        services.AddDomium();

        await using var provider = services.BuildServiceProvider();
        var internalPublisher = provider.GetRequiredService<IInternalEventPublisher>();
        var externalPublisher = provider.GetRequiredService<IExternalEventPublisher>();

        await internalPublisher.PublishAsync(new InternalPingEvent("internal"));
        await externalPublisher.PublishAsync(new ExternalPingEvent("external"));

        Assert.Equal("internal", InternalPingHandler.LastMessage);
    }

    [Fact]
    public async Task EventBus_uses_internal_and_domain_event_handlers()
    {
        InternalPingHandler.Reset();
        DomainPingHandler.Reset();
        var services = new ServiceCollection();

        services.AddDomium();

        await using var provider = services.BuildServiceProvider();
        var eventBus = provider.GetRequiredService<IEventBus>();

        await eventBus.PublishAsync(new InternalPingEvent("domain"));

        Assert.Equal("domain", InternalPingHandler.LastMessage);
        Assert.Equal("domain", DomainPingHandler.LastMessage);
    }

    [Fact]
    public async Task EventBus_publishes_domain_events_to_application_handlers_in_memory()
    {
        InternalPingHandler.Reset();
        DomainPingHandler.Reset();
        var services = new ServiceCollection();

        services.AddDomium();

        await using var provider = services.BuildServiceProvider();
        var eventBus = provider.GetRequiredService<IEventBus>();

        await eventBus.PublishAsync(new InternalPingEvent("event-bus"));

        Assert.Equal("event-bus", InternalPingHandler.LastMessage);
        Assert.Equal("event-bus", DomainPingHandler.LastMessage);
    }

    public sealed class InternalPingEvent(string message) : DomainEvent
    {
        public string Message { get; } = message;
    }

    public sealed class ExternalPingEvent(string message) : IExternalEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();

        public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;

        public string Message { get; } = message;
    }

    public sealed class InternalPingHandler : IInternalEventHandler<InternalPingEvent>
    {
        public static string? LastMessage { get; private set; }

        public static void Reset()
        {
            LastMessage = null;
        }

        public Task HandleAsync(
            InternalPingEvent internalEvent,
            CancellationToken cancellationToken = default)
        {
            LastMessage = internalEvent.Message;
            return Task.CompletedTask;
        }
    }

    public sealed class DomainPingHandler : IDomainEventHandler<InternalPingEvent>
    {
        public static string? LastMessage { get; private set; }

        public static void Reset()
        {
            LastMessage = null;
        }

        public Task HandleAsync(
            InternalPingEvent domainEvent,
            CancellationToken cancellationToken = default)
        {
            LastMessage = domainEvent.Message;
            return Task.CompletedTask;
        }
    }
}
