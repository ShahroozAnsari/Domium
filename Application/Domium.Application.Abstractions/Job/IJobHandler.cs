using Domium.Application.Abstractions.Command;

namespace Domium.Application.Abstractions.Job;

public interface IJobHandler<in TJob> : ICommandHandler<TJob>
    where TJob : IJob
{
}
