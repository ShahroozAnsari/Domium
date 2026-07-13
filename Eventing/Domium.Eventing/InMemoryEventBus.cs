using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
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
/// Handler invocation goes through compiled delegates cached per event type — no per-publish
/// reflection — and handler exceptions surface with their original stack trace.
/// </summary>
public sealed class InMemoryEventBus(
    IServiceProvider serviceProvider,
    IExternalEventPublisher externalEventPublisher) : IEventBus
{
    private static readonly ConcurrentDictionary<(Type OpenHandlerType, Type EventType), HandlerInvoker> Invokers = new();
    private static readonly ConcurrentDictionary<Type, ExternalPublishInvoker> ExternalInvokers = new();

    private delegate Task HandlerInvoker(object handler, IDomiumEvent @event, CancellationToken cancellationToken);
    private delegate Task ExternalPublishInvoker(IExternalEventPublisher publisher, IExternalEvent @event, CancellationToken cancellationToken);

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
            await DispatchAsync(typeof(IInternalEventHandler<>), @event, cancellationToken).ConfigureAwait(false);
            DomiumTelemetry.InternalEventsPublished.Add(
                1,
                new KeyValuePair<string, object?>("domium.event.name", eventName));
        }

        if (@event is IDomainEvent)
        {
            await DispatchAsync(typeof(IDomainEventHandler<>), @event, cancellationToken).ConfigureAwait(false);
        }

        if (@event is IExternalEvent externalEvent)
        {
            await PublishExternalAsync(externalEvent, cancellationToken).ConfigureAwait(false);
        }
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
        IDomiumEvent @event,
        CancellationToken cancellationToken)
    {
        var invoker = Invokers.GetOrAdd((openHandlerType, @event.GetType()), BuildHandlerInvoker);
        var handlerType = openHandlerType.MakeGenericType(@event.GetType());

        foreach (var handler in serviceProvider.GetServices(handlerType))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (handler is not null)
            {
                await invoker(handler, @event, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private Task PublishExternalAsync(IExternalEvent externalEvent, CancellationToken cancellationToken)
    {
        var invoker = ExternalInvokers.GetOrAdd(externalEvent.GetType(), BuildExternalInvoker);
        return invoker(externalEventPublisher, externalEvent, cancellationToken);
    }

    /// <summary>
    /// Compiles: (handler, event, ct) => ((THandler)handler).HandleAsync((TEvent)event, ct).
    /// </summary>
    private static HandlerInvoker BuildHandlerInvoker((Type OpenHandlerType, Type EventType) key)
    {
        var handlerType = key.OpenHandlerType.MakeGenericType(key.EventType);
        var handleMethod = handlerType.GetMethod("HandleAsync")
            ?? throw new InvalidOperationException($"{handlerType.Name} does not define HandleAsync.");

        var handler = Expression.Parameter(typeof(object), "handler");
        var @event = Expression.Parameter(typeof(IDomiumEvent), "event");
        var cancellationToken = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

        var call = Expression.Call(
            Expression.Convert(handler, handlerType),
            handleMethod,
            Expression.Convert(@event, key.EventType),
            cancellationToken);

        return Expression
            .Lambda<HandlerInvoker>(call, handler, @event, cancellationToken)
            .Compile();
    }

    /// <summary>
    /// Compiles: (publisher, event, ct) => publisher.PublishAsync&lt;TEvent&gt;((TEvent)event, ct).
    /// </summary>
    private static ExternalPublishInvoker BuildExternalInvoker(Type eventType)
    {
        var publishMethod = typeof(IExternalEventPublisher)
            .GetMethod(nameof(IExternalEventPublisher.PublishAsync))!
            .MakeGenericMethod(eventType);

        var publisher = Expression.Parameter(typeof(IExternalEventPublisher), "publisher");
        var @event = Expression.Parameter(typeof(IExternalEvent), "event");
        var cancellationToken = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

        var call = Expression.Call(
            publisher,
            publishMethod,
            Expression.Convert(@event, eventType),
            cancellationToken);

        return Expression
            .Lambda<ExternalPublishInvoker>(call, publisher, @event, cancellationToken)
            .Compile();
    }
}
