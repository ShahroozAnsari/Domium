using System.Diagnostics;
using Domium.Application.Abstractions.Query;
using Domium.Application.Abstractions.Query.Pipelines;
using Microsoft.Extensions.DependencyInjection;

namespace Domium.Application.Query;

public sealed class QueryBus(IServiceProvider serviceProvider) : IQueryBus
{
    public async Task<TResult> ExecuteAsync<TQuery, TResult>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : class, IQuery<TResult>
        where TResult : class
    {
        if (query == null) throw new ArgumentNullException(nameof(query));

        var queryName = typeof(TQuery).FullName ?? typeof(TQuery).Name;
        var stopwatch = Stopwatch.StartNew();

        var handler = serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResult>>();
        var behaviors = serviceProvider
            .GetServices<IQueryPipelineBehavior<TQuery, TResult>>()
            .Reverse()
            .ToArray();

        QueryHandlerDelegate<TResult> pipeline = () => handler.HandleAsync(query, cancellationToken);

        foreach (var behavior in behaviors)
        {
            var next = pipeline;
            pipeline = () => behavior.HandleAsync(query, cancellationToken, next);
        }

        return await pipeline().ConfigureAwait(false);



    }
}
