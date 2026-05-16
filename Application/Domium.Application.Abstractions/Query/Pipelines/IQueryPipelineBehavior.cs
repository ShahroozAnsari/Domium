using System.Threading;
using System.Threading.Tasks;

namespace Domium.Application.Abstractions.Query.Pipelines;

public interface IQueryPipelineBehavior<in TQuery, TResult>
    where TQuery : IQuery<TResult> where TResult : class
{
    Task<TResult> HandleAsync(
        TQuery query,
        CancellationToken cancellationToken,
        QueryHandlerDelegate<TResult> next);
}