using Domium.Application.Abstractions.Query;
using Domium.Facade.Abstractions;

namespace Domium.Facade;

public sealed class DomiumQueryFacade(IQueryBus queryBus) : IQueryFacade
{
    public Task<TResult> QueryAsync<TQuery, TResult>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : class, IQuery<TResult>
        where TResult : class
    {
        return queryBus.ExecuteAsync<TQuery, TResult>(query, cancellationToken);
    }
}
