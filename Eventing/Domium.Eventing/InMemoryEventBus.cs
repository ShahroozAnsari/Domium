using System.Diagnostics;
using System.Reflection;
using Domium.Application.Abstractions.Events;
using Domium.Domain.Abstractions.Events;
using Domium.Eventing.Abstractions;
using Domium.Eventing.Abstractions.External;
using Domium.Eventing.Abstractions.Internal;
using Domium.Observability;
using Microsoft.Extensions.DependencyInjection;

namespace Domium.Eventing;

/// <summary>
/// In-memory event bus that invokes handlers registered in the current service provider.
/// </summary>
public sealed class InMemoryEventBus(
    IServiceProvider serviceProvider,
    IExternalEventPublisher externalEventPublisher) : IEventBus
{
    public async Task PublishAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default)
        where TEvent : IDomiumEvent
    {
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        var eventName = @event.GetType().FullName ?? @event.GetType().Name;

        using var activity = DomiumTelemetry.ActivitySource.StartActivity(
            "domium.event.publish",
            ActivityKind.Internal);

        activity?.SetTag("domium.event.name", eventName);
        activity?.SetTag("domium.event.id", @event.EventId);

        if (@event is IInternalEvent)
        {
            await DispatchAsync(
                typeof(IInternalEventHandler<>),
                nameof(IInternalEventHandler<IInternalEvent>.HandleAsync),
                @event,
                cancellationToken).ConfigureAwait(false);
        }

        if (@event is IDomainEvent)
        {
            await DispatchAsync(
                typeof(IDomainEventHandler<>),
                nameof(IDomainEventHandler<IDomainEvent>.HandleAsync),
                @event,
                cancellationToken).ConfigureAwait(false);
        }

        if (@event is IExternalEvent externalEvent)
        {
            await PublishExternalAsync(externalEvent, cancellationToken).ConfigureAwait(false);
        }

        DomiumTelemetry.InternalEventsPublished.Add(
            1,
            new KeyValuePair<string, object?>("domium.event.name", eventName));
    }

    public async Task PublishAsync(
        IReadOnlyCollection<IDomiumEvent> events,
        CancellationToken cancellationToken = default)
    {
        if (events == null)
        {
            throw new ArgumentNullException(nameof(events));
        }

        foreach (var @event in events)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await PublishAsync(@event, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task DispatchAsync(
        Type openHandlerType,
        string handleMethodName,
        IDomiumEvent @event,
        CancellationToken cancellationToken)
    {
        var handlerType = openHandlerType.MakeGenericType(@event.GetType());
        var handlers = serviceProvider.GetServices(handlerType);
        var handleMethod = handlerType.GetMethod(handleMethodName);

        if (handleMethod is null)
        {
            return;
        }

        foreach (var handler in handlers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Task? task;

            try
            {
                task = (Task?)handleMethod.Invoke(handler, new object[] { @event, cancellationToken });
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                throw exception.InnerException;
            }

            if (task is not null)
            {
                await task.ConfigureAwait(false);
            }
        }
    }

    private async Task PublishExternalAsync(
        IExternalEvent externalEvent,
        CancellationToken cancellationToken)
    {
        var publishMethod = typeof(IExternalEventPublisher)
            .GetMethod(nameof(IExternalEventPublisher.PublishAsync))?
            .MakeGenericMethod(externalEvent.GetType());

        if (publishMethod is null)
        {
            return;
        }

        Task? task;

        try
        {
            task = (Task?)publishMethod.Invoke(
                externalEventPublisher,
                new object[] { externalEvent, cancellationToken });
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw exception.InnerException;
        }

        if (task is not null)
        {
            await task.ConfigureAwait(false);
        }
    }
}
