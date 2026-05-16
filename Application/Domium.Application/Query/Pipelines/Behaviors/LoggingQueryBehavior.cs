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
        var queryName = typeof(TQuery).Name;

        _logger.LogInformation("Executing query {QueryName}", queryName);

        var result = await next();

        _logger.LogInformation("Executed query {QueryName}", queryName);

        return result;
    }
}