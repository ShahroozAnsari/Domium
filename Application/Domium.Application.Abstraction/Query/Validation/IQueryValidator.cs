using System.Threading;
using System.Threading.Tasks;

namespace Domium.Application.Abstraction.Query.Validation;

public interface IQueryValidator<in TQuery, TResult>
{
    Task ValidateAsync(TQuery query, CancellationToken cancellationToken = default);
}