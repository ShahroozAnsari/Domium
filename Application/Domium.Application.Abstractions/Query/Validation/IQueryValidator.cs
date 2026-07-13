using System.Threading;
using System.Threading.Tasks;

namespace Domium.Application.Abstractions.Query.Validation;

public interface IQueryValidator<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task ValidateAsync(TQuery query, CancellationToken cancellationToken = default);
}
