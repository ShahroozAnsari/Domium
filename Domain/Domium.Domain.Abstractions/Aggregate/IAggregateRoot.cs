using Domium.Domain.Abstractions.Entity;

namespace Domium.Domain.Abstractions.Aggregate;

public interface IAggregateRoot : IEntityBase
{
}

public interface IAggregateRoot<TId> : IAggregateRoot, IEntityBase<TId>
    where TId : IAggregateId
{
}
