using Domium.Application.Abstractions.Query;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domium.Facade
{
    public class DomiumQueryFacade(IQueryBus queryBus)
    {
        protected Task<TResult> QueryAsync<TQuery, TResult>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : class, IQuery<TResult>
        where TResult : class
        {
            return queryBus.ExecuteAsync<TQuery, TResult>(query, cancellationToken);
        }
    }
}
