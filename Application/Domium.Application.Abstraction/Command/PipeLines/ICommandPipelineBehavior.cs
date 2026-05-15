using System.Threading;
using System.Threading.Tasks;

namespace Domium.Application.Abstraction.Command.PipeLines;

public interface ICommandPipelineBehavior<in TCommand>
    where TCommand : ICommand
{
    Task HandleAsync(
        TCommand command,
        CancellationToken cancellationToken,
        CommandHandlerDelegate next);
}