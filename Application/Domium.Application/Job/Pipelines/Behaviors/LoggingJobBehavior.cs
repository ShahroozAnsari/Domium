using Domium.Application.Abstractions.Job;
using Domium.Application.Abstractions.Job.Pipelines;
using Microsoft.Extensions.Logging;

namespace Domium.Application.Job.Pipelines.Behaviors;

public sealed class LoggingJobBehavior<TJob>(ILogger<LoggingJobBehavior<TJob>> logger)
    : IJobPipelineBehavior<TJob>
    where TJob : IJob
{
    private readonly ILogger<LoggingJobBehavior<TJob>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task HandleAsync(
        TJob job,
        CancellationToken cancellationToken,
        JobHandlerDelegate next)
    {
        if (job == null) throw new ArgumentNullException(nameof(job));
        if (next == null) throw new ArgumentNullException(nameof(next));

        var jobName = typeof(TJob).Name;

        _logger.LogInformation("Executing job {JobName}", jobName);

        try
        {
            await next().ConfigureAwait(false);
            _logger.LogInformation("Executed job {JobName}", jobName);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Job {JobName} failed", jobName);
            throw;
        }
    }
}
