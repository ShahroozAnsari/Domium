using Domium.Domain.Abstractions.Aggregate;
using Domium.Domain.Abstractions.Entity;
using Domium.Domain.Abstractions.Events;
using Microsoft.EntityFrameworkCore;

namespace Domium.Persistence.EntityFrameworkCore;

/// <summary>
/// Base EF Core DbContext with optional domain event dispatch after saving changes.
/// </summary>
public abstract class DomiumDbContext : DbContext
{
    private readonly IDomainEventDispatcher? _domainEventDispatcher;
    private bool _suppressDomainEventDispatch;

    protected DomiumDbContext(DbContextOptions options)
        : base(options)
    {
    }

    protected DomiumDbContext(
        DbContextOptions options,
        IDomainEventDispatcher? domainEventDispatcher)
        : base(options)
    {
        _domainEventDispatcher = domainEventDispatcher;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyEntityMetadata();

        var domainEvents = CaptureDomainEvents();
        var result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (!_suppressDomainEventDispatch && domainEvents.Events.Count > 0)
        {
            await DispatchDomainEventsAsync(domainEvents, cancellationToken).ConfigureAwait(false);
            ClearDomainEvents(domainEvents);
        }

        return result;
    }

    private void ApplyEntityMetadata()
    {
        var timestamp = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDeletableEntity)
            {
                InvokeEntityMethod(entry.Entity, "MarkDeleted", timestamp, null);
                entry.State = EntityState.Modified;
            }

            if (entry.Entity is not IAuditableEntity)
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                InvokeEntityMethod(entry.Entity, "MarkCreated", timestamp, null);
            }
            else if (entry.State == EntityState.Modified)
            {
                InvokeEntityMethod(entry.Entity, "MarkModified", timestamp, null);
            }
        }
    }

    private static void InvokeEntityMethod(
        object entity,
        string methodName,
        DateTimeOffset timestamp,
        string? actor)
    {
        var method = entity.GetType().GetMethod(
            methodName,
            new[] { typeof(DateTimeOffset), typeof(string) });

        method?.Invoke(entity, new object?[] { timestamp, actor });
    }

    internal DomainEventBatch CaptureDomainEvents()
    {
        var aggregateRoots = ChangeTracker
            .Entries<IAggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToArray();

        var domainEvents = aggregateRoots
            .SelectMany(aggregate => aggregate.DomainEvents)
            .ToArray();

        return new DomainEventBatch(aggregateRoots, domainEvents);
    }

    internal async Task DispatchDomainEventsAsync(
        DomainEventBatch domainEvents,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        if (_domainEventDispatcher is null || domainEvents.Events.Count == 0)
        {
            return;
        }

        await _domainEventDispatcher
            .DispatchAsync(domainEvents.Events, cancellationToken)
            .ConfigureAwait(false);
    }

    internal static void ClearDomainEvents(DomainEventBatch domainEvents)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (var aggregateRoot in domainEvents.AggregateRoots)
        {
            aggregateRoot.ClearDomainEvents();
        }
    }

    internal IDisposable SuppressDomainEventDispatch()
    {
        var previous = _suppressDomainEventDispatch;
        _suppressDomainEventDispatch = true;
        return new DispatchSuppressionScope(this, previous);
    }

    internal sealed class DomainEventBatch(
        IReadOnlyCollection<IAggregateRoot> aggregateRoots,
        IReadOnlyCollection<IDomainEvent> events)
    {
        public IReadOnlyCollection<IAggregateRoot> AggregateRoots { get; } = aggregateRoots;

        public IReadOnlyCollection<IDomainEvent> Events { get; } = events;
    }

    private sealed class DispatchSuppressionScope(
        DomiumDbContext dbContext,
        bool previous)
        : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            dbContext._suppressDomainEventDispatch = previous;
            _disposed = true;
        }
    }
}
