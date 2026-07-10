
using Domium.Domain.Abstractions.Aggregate;

namespace Domium.Domain.Abstractions.DomainService;


public interface IRepository<TAggregate, TId>
    where TAggregate : class, IAggregateRoot<TId>
    where TId : IAggregateId
{

}
