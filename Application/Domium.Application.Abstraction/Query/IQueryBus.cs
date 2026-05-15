using System.Threading;
using System.Threading.Tasks;

namespace Domium.Application.Abstraction.Query;


public interface IQueryBus
{
    Task<TResult> ExecuteAsync<TQuery, TResult>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery :class, IQuery<TResult> where TResult:class;
}
