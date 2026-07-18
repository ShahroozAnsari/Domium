using Domium.Application.Abstractions.Job;
using Domium.Application.Abstractions.Job.Pipelines;
using Domium.Persistence.Abstractions;

namespace Domium.Application.Job.Pipelines.Behaviors;

public sealed class TransactionJobBehavior<TJob>(IUnitOfWork unitOfWork) : IJobPipelineBehavior<TJob>
    where TJob : IJob
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    public Task HandleAsync(
        TJob job,
        CancellationToken cancellationToken,
        JobHandlerDelegate next)
    {
        if (job == null) throw new ArgumentNullException(nameof(job));
        if (next == null) throw new ArgumentNullException(nameof(next));

        return _unitOfWork.ExecuteAsync(() => next(), cancellationToken);
    }
}
