using System.Threading;
using System.Threading.Tasks;

namespace Domium.Application.Abstractions.Job;

public interface IJobBus
{
    Task ExecuteAsync<TJob>(TJob job, CancellationToken cancellationToken = default)
        where TJob : IJob;
}
