using System.Threading;
using System.Threading.Tasks;

namespace Domium.Application.Abstractions.Job;

public interface IJobHandler<in TJob>
    where TJob : IJob
{
    Task HandleAsync(TJob job, CancellationToken cancellationToken = default);
}
