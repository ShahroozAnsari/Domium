using Domium.Domain;
using Domium.Eventing;
using Domium.Eventing.Abstractions;
using Domium.Eventing.Abstractions.External;
using Microsoft.Extensions.DependencyInjection;

namespace Domium.Tests.Eventing;

public sealed class EventBusSubscriptionTests
{
    [Fact]
    public async Task Subscriber_receives_the_published_event()
    {
        var bus = CreateBus();
        var captured = Guid.Empty;

        using (bus.Subscribe<ThingCreated>((@event, _) =>
        {
            captured = @event.ThingId;
            return Task.CompletedTask;
        }))
        {
            await bus.PublishAsync(new ThingCreated(Guid.Parse("11111111-1111-1111-1111-111111111111")));
        }

        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), captured);
    }

    [Fact]
    public async Task Disposing_the_subscription_stops_delivery()
    {
        var bus = CreateBus();
        var count = 0;

        var subscription = bus.Subscribe<ThingCreated>((_, _) =>
        {
            count++;
            return Task.CompletedTask;
        });

        await bus.PublishAsync(new ThingCreated(Guid.NewGuid()));
        subscription.Dispose();
        await bus.PublishAsync(new ThingCreated(Guid.NewGuid()));

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Subscriber_ignores_events_of_other_types()
    {
        var bus = CreateBus();
        var received = false;

        using var subscription = bus.Subscribe<ThingCreated>((_, _) =>
        {
            received = true;
            return Task.CompletedTask;
        });

        await bus.PublishAsync(new OtherThingHappened());

        Assert.False(received);
    }

    [Fact]
    public async Task Publishing_without_subscribers_does_not_throw()
    {
        var bus = CreateBus();

        var exception = await Record.ExceptionAsync(() => bus.PublishAsync(new ThingCreated(Guid.NewGuid())));

        Assert.Null(exception);
    }

    private static IEventBus CreateBus()
    {
        var services = new ServiceCollection();
        services.AddDomiumEventing();

        var provider = services.BuildServiceProvider();

        return new InMemoryEventBus(provider, provider.GetRequiredService<IExternalEventPublisher>());
    }

    private sealed class ThingCreated(Guid thingId) : DomainEvent
    {
        public Guid ThingId { get; } = thingId;
    }

    private sealed class OtherThingHappened : DomainEvent;
}
