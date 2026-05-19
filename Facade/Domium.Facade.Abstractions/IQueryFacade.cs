using System.Threading;
using System.Threading.Tasks;
using Domium.Application.Abstractions.Query;

namespace Domium.Facade.Abstractions;

public interface IQueryFacade
{
    Task<TResult> QueryAsync<TQuery, TResult>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : class, IQuery<TResult>
        where TResult : class;
}
