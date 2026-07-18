using System.Threading;
using System.Threading.Tasks;

namespace Domium.Application.Abstractions.Job.Pipelines;

public interface IJobPipelineBehavior<in TJob>
    where TJob : IJob
{
    Task HandleAsync(
        TJob job,
        CancellationToken cancellationToken,
        JobHandlerDelegate next);
}
