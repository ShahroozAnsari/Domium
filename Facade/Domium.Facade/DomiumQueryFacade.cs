using Domium.Application.Abstractions.Query;
using Domium.Facade.Abstractions;

namespace Domium.Facade;

public abstract class DomiumQueryFacade(IQueryBus queryBus) : IFacade
{
    protected Task<TResult> QueryAsync<TQuery, TResult>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>
    {
        return queryBus.ExecuteAsync<TQuery, TResult>(query, cancellationToken);
    }
}
