using Domium.Domain.Abstractions.Aggregate;
using Domium.Domain.Abstractions.Events;
using Microsoft.EntityFrameworkCore;

namespace Domium.Persistence.EntityFrameworkCore;

/// <summary>
/// Base EF Core DbContext with optional domain event dispatch after saving changes.
/// </summary>
public abstract class DomiumDbContext : DbContext
{
    private readonly IDomainEventDispatcher? _domainEventDispatcher;

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
        var domainEvents = CollectDomainEvents();
        var result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (_domainEventDispatcher is not null && domainEvents.Count > 0)
        {
            await _domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    private IReadOnlyCollection<IDomainEvent> CollectDomainEvents()
    {
        var aggregateRoots = ChangeTracker
            .Entries<IAggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToArray();

        var domainEvents = aggregateRoots
            .SelectMany(aggregate => aggregate.DomainEvents)
            .ToArray();

        foreach (var aggregateRoot in aggregateRoots)
        {
            aggregateRoot.ClearDomainEvents();
        }

        return domainEvents;
    }
}
