using System.Threading;
using System.Threading.Tasks;
using Domium.Domain.Abstractions.Aggregate;

namespace Domium.Persistence.Abstractions;

/// <summary>
/// Provider-neutral aggregate repository: the four operations every persistence provider
/// (EF Core, Dapper, ...) can honor. Specification-based reads live on
/// <see cref="ISpecificationRepository{TAggregate,TId}"/>, which LINQ-capable providers add.
/// </summary>
public interface IRepository<TAggregate, in TId>
    where TAggregate : class, IAggregateRoot<TId>
    where TId : IAggregateId
{

}
