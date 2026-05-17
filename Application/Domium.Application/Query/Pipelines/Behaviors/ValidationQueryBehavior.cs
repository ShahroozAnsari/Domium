using Domium.Application.Abstractions.Query;
using Domium.Application.Abstractions.Query.Pipelines;
using Domium.Application.Abstractions.Query.Validation;

namespace Domium.Application.Query.Pipelines.Behaviors;

public sealed class ValidationQueryBehavior<TQuery, TResult>(IEnumerable<IQueryValidator<TQuery, TResult>> validators)
    : IQueryPipelineBehavior<TQuery, TResult>
    where TQuery : class, IQuery<TResult> where TResult : class
{
    public async Task<TResult> HandleAsync(
        TQuery query,
        CancellationToken cancellationToken,
        QueryHandlerDelegate<TResult> next)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (next == null) throw new ArgumentNullException(nameof(next));

        foreach (var validator in validators)
        {
            await validator.ValidateAsync(query, cancellationToken).ConfigureAwait(false);
        }

        return await next().ConfigureAwait(false);
    }
}
