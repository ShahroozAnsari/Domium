using System.Threading;
using System.Threading.Tasks;

namespace Domium.Application.Abstraction.Command;

public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}