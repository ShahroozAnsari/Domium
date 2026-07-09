using Domium.Domain.Abstractions.Aggregate;

namespace Domium.Persistence.Dapper;

/// <summary>
/// Maps an aggregate to explicit SQL commands for the Dapper repository.
/// </summary>
public interface IDapperAggregateMapper<TAggregate, TId>
    where TAggregate : class, IAggregateRoot<TId>
    where TId : IAggregateId
{
    string SelectByIdSql { get; }

    string InsertSql { get; }

    string UpdateSql { get; }

    string DeleteSql { get; }

    object GetIdParameters(TId id);

    object GetInsertParameters(TAggregate aggregate);

    object GetUpdateParameters(TAggregate aggregate);

    object GetDeleteParameters(TAggregate aggregate);

    TAggregate Map(object row);
}
