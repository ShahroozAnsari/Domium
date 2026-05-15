using System.Threading;
using System.Threading.Tasks;

namespace Domium.Application.Abstraction.Command;

public interface ICommandBus
{
    Task ExecuteAsync<TCommand>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand;
}