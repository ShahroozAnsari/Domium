using Domium.Application.Abstractions.Query;
using Domium.Application.Abstractions.Query.Pipelines;
using Microsoft.Extensions.DependencyInjection;

namespace Domium.Application.Query;

/// <summary>
/// Resolves the handler and pipeline behaviors for a query and executes them.
/// Cross-cutting concerns (observability, logging, validation, caching) are pipeline
/// behaviors — the bus itself only builds and invokes the chain.
/// </summary>
public sealed class QueryBus(IServiceProvider serviceProvider) : IQueryBus
{
    public Task<TResult> ExecuteAsync<TQuery, TResult>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>
    {
        if (query == null) throw new ArgumentNullException(nameof(query));

        var handler = serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResult>>();
        var behaviors = serviceProvider.GetServices<IQueryPipelineBehavior<TQuery, TResult>>().ToArray();

        QueryHandlerDelegate<TResult> pipeline = () => handler.HandleAsync(query, cancellationToken);

        // Wrap innermost-first so the first-registered behavior ends up outermost.
        for (var i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var next = pipeline;
            pipeline = () => behavior.HandleAsync(query, cancellationToken, next);
        }

        return pipeline();
    }
}
