using System.Diagnostics;
using Domium.Application.Abstractions.Job;
using Domium.Application.Abstractions.Job.Pipelines;
using Domium.Observability;

namespace Domium.Application.Job.Pipelines.Behaviors;

public sealed class ObservabilityJobBehavior<TJob> : IJobPipelineBehavior<TJob>
    where TJob : IJob
{
    public async Task HandleAsync(
        TJob job,
        CancellationToken cancellationToken,
        JobHandlerDelegate next)
    {
        if (job == null) throw new ArgumentNullException(nameof(job));
        if (next == null) throw new ArgumentNullException(nameof(next));

        var jobName = typeof(TJob).FullName ?? typeof(TJob).Name;
        var stopwatch = Stopwatch.StartNew();

        using var activity = DomiumTelemetry.ActivitySource.StartActivity(
            "domium.job.execute",
            ActivityKind.Internal);

        activity?.SetTag("domium.job.name", jobName);

        try
        {
            await next().ConfigureAwait(false);
            DomiumTelemetry.JobsExecuted.Add(
                1,
                new KeyValuePair<string, object?>("domium.job.name", jobName));
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
                new KeyValuePair<string, object?>("domium.operation.type", "job"),
                new KeyValuePair<string, object?>("domium.job.name", jobName));
        }
    }
}
