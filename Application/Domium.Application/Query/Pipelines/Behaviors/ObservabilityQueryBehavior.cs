using System.Diagnostics;
using Domium.Application.Abstractions.Query;
using Domium.Application.Abstractions.Query.Pipelines;
using Domium.Observability;

namespace Domium.Application.Query.Pipelines.Behaviors;

/// <summary>
/// Wraps query execution in an Activity span, counts executed queries, and records the
/// operation duration. Registered first, so the span covers every other behavior (including
/// caching) and the handler itself.
/// </summary>
public sealed class ObservabilityQueryBehavior<TQuery, TResult> : IQueryPipelineBehavior<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    public async Task<TResult> HandleAsync(
        TQuery query,
        CancellationToken cancellationToken,
        QueryHandlerDelegate<TResult> next)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (next == null) throw new ArgumentNullException(nameof(next));

        var queryName = typeof(TQuery).FullName ?? typeof(TQuery).Name;
        var stopwatch = Stopwatch.StartNew();

        using var activity = DomiumTelemetry.ActivitySource.StartActivity(
            "domium.query.execute",
            ActivityKind.Internal);

        activity?.SetTag("domium.query.name", queryName);

        try
        {
            var result = await next().ConfigureAwait(false);
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
