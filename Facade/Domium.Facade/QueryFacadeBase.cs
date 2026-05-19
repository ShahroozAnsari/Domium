using Domium.Application.Abstractions.Query;
using Domium.Facade.Abstractions;

namespace Domium.Facade;

public abstract class QueryFacadeBase(IQueryFacade facade)
{
    protected Task<TResult> QueryAsync<TQuery, TResult>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : class, IQuery<TResult>
        where TResult : class
    {
        return facade.QueryAsync<TQuery, TResult>(query, cancellationToken);
    }
}
