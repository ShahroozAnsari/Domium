using System.Diagnostics;
using Domium.Eventing.Abstractions.External;
using Domium.Observability;
using MassTransit;

namespace Domium.Eventing.MassTransit;

/// <summary>
/// Publishes Domium external events through MassTransit.
/// </summary>
public sealed class MassTransitExternalEventPublisher(IPublishEndpoint publishEndpoint) : IExternalEventPublisher
{
    public async Task PublishAsync<TExternalEvent>(
        TExternalEvent externalEvent,
        CancellationToken cancellationToken = default)
        where TExternalEvent : class, IExternalEvent
    {
        if (externalEvent == null)
        {
            throw new ArgumentNullException(nameof(externalEvent));
        }

        var eventName = typeof(TExternalEvent).FullName ?? typeof(TExternalEvent).Name;

        using var activity = DomiumTelemetry.ActivitySource.StartActivity(
            "domium.external_event.publish",
            ActivityKind.Producer);

        activity?.SetTag("domium.event.name", eventName);
        activity?.SetTag("domium.event.id", externalEvent.EventId);

        try
        {
            await publishEndpoint.Publish(externalEvent, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            throw;
        }

        // Counted only after the broker accepted the message, so the metric reflects
        // events actually published rather than attempts.
        DomiumTelemetry.ExternalEventsPublished.Add(
            1,
            new KeyValuePair<string, object?>("domium.event.name", eventName),
            new KeyValuePair<string, object?>("domium.event.provider", "masstransit"));
    }
}
