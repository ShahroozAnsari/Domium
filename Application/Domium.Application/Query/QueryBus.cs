using System.Diagnostics;
using Domium.Application.Abstractions.Query;
using Domium.Application.Abstractions.Query.Pipelines;
using Domium.Observability;
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

        using var activity = DomiumTelemetry.ActivitySource.StartActivity(
            "domium.query.execute",
            ActivityKind.Internal);

        activity?.SetTag("domium.query.name", queryName);

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

        try
        {
            var result = await pipeline().ConfigureAwait(false);
            DomiumTelemetry.QueriesExecuted.Add(
                1,
                new KeyValuePair<string, object?>("domium.query.name", queryName));
            return result;
        }
        catch (Exception exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity?.SetTag("exception.type", exception.GetType().FullName);
            activity?.SetTag("exception.message", exception.Message);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            DomiumTelemetry.OperationDuration.Record(
                stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("domium.operation.type", "query"),
                new KeyValuePair<string, object?>("domium.query.name", queryName));
        }
    }
}
