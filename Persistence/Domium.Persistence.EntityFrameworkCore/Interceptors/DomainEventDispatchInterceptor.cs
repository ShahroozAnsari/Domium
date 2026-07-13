using Domium.Eventing.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Domium.Persistence.EntityFrameworkCore;

/// <summary>
/// Publishes buffered domain events (raised by aggregates that had no <see cref="IEventBus"/>
/// attached yet — typically freshly created ones) right before SaveChanges. Handlers run in
/// the same DI scope and use the same DbContext, so their changes are persisted by this very
/// SaveChanges call — the aggregate change and its event handlers commit atomically.
/// Handlers must not call SaveChanges themselves.
/// </summary>
public sealed class DomainEventDispatchInterceptor(IEventBus eventBus) : SaveChangesInterceptor
{
    private const int MaxDispatchRounds = 10;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        DispatchPendingEventsAsync(eventData.Context, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await DispatchPendingEventsAsync(eventData.Context, cancellationToken).ConfigureAwait(false);
        return await base.SavingChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }

    private async Task DispatchPendingEventsAsync(DbContext? dbContext, CancellationToken cancellationToken)
    {
        if (dbContext is null)
        {
            return;
        }

        // Handlers may create new aggregates that buffer more events; keep draining until
        // a round produces nothing (bounded to protect against event loops).
        for (var round = 0; round < MaxDispatchRounds; round++)
        {
            var pending = dbContext.ChangeTracker
                .Entries()
                .Select(entry => entry.Entity)
                .OfType<IDomiumEventSource>()
                .SelectMany(source => source.DequeuePendingDomainEvents())
                .ToArray();

            if (pending.Length == 0)
            {
                return;
            }

            await eventBus.PublishAsync(pending, cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException(
            $"Domain event dispatch did not settle after {MaxDispatchRounds} rounds; " +
            "an event handler appears to raise events in a loop.");
    }
}
