using Domium.Eventing.Abstractions.External;
using Domium.Eventing.MassTransit;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Domium.Tests.Eventing;

public sealed class MassTransitEventingTests
{
    [Fact]
    public async Task MassTransit_external_publisher_publishes_to_domium_external_event_handler()
    {
        ExternalPingHandler.Reset();
        var services = new ServiceCollection();

        services.AddScoped<IExternalEventHandler<ExternalPingEvent>, ExternalPingHandler>();
        services.AddDomiumMassTransitEventing();
        services.AddMassTransit(configurator =>
        {
            configurator.AddDomiumExternalEventConsumer<ExternalPingEvent>();
            configurator.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
        });

        await using var provider = services.BuildServiceProvider(true);
        var bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync();

        try
        {
            using var scope = provider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IExternalEventPublisher>();

            await publisher.PublishAsync(new ExternalPingEvent("via-masstransit"));

            await WaitUntilAsync(() => ExternalPingHandler.LastMessage == "via-masstransit");
        }
        finally
        {
            await bus.StopAsync();
        }

        Assert.Equal("via-masstransit", ExternalPingHandler.LastMessage);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        while (!condition())
        {
            cancellationTokenSource.Token.ThrowIfCancellationRequested();
            await Task.Delay(50, cancellationTokenSource.Token);
        }
    }

    public sealed class ExternalPingEvent(string message) : IExternalEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();

        public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;

        public string Message { get; } = message;
    }

    public sealed class ExternalPingHandler : IExternalEventHandler<ExternalPingEvent>
    {
        public static string? LastMessage { get; private set; }

        public static void Reset()
        {
            LastMessage = null;
        }

        public Task HandleAsync(
            ExternalPingEvent externalEvent,
            CancellationToken cancellationToken = default)
        {
            LastMessage = externalEvent.Message;
            return Task.CompletedTask;
        }
    }
}
