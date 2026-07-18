using Domium.Application.Abstractions.Job;
using Domium.Application.Abstractions.Job.Pipelines;
using Microsoft.Extensions.DependencyInjection;

namespace Domium.Application.Job;

public sealed class JobBus(IServiceProvider serviceProvider) : IJobBus
{
    public Task ExecuteAsync<TJob>(TJob job, CancellationToken cancellationToken = default)
        where TJob : IJob
    {
        if (job == null) throw new ArgumentNullException(nameof(job));

        var handler = serviceProvider.GetRequiredService<IJobHandler<TJob>>();
        var behaviors = serviceProvider.GetServices<IJobPipelineBehavior<TJob>>().ToArray();

        JobHandlerDelegate pipeline = () => handler.HandleAsync(job, cancellationToken);

        for (var i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var next = pipeline;
            pipeline = () => behavior.HandleAsync(job, cancellationToken, next);
        }

        return pipeline();
    }
}
