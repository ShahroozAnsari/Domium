using Domium.Application.Abstractions.Query;
using Domium.Application.Abstractions.Query.Pipelines;
using Microsoft.Extensions.Logging;

namespace Domium.Application.Query.Pipelines.Behaviors;

public sealed class LoggingQueryBehavior<TQuery, TResult>(ILogger<LoggingQueryBehavior<TQuery, TResult>> logger)
    : IQueryPipelineBehavior<TQuery, TResult>
    where TQuery :class, IQuery<TResult> where TResult : class
{
    private readonly ILogger<LoggingQueryBehavior<TQuery, TResult>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<TResult> HandleAsync(
        TQuery query,
        CancellationToken cancellationToken,
        QueryHandlerDelegate<TResult> next)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (next == null) throw new ArgumentNullException(nameof(next));

        var queryName = typeof(TQuery).Name;

        _logger.LogInformation("Executing query {QueryName}", queryName);

        try
        {
            var result = await next().ConfigureAwait(false);
            _logger.LogInformation("Executed query {QueryName}", queryName);
            return result;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Query {QueryName} failed", queryName);
            throw;
        }
    }
}
