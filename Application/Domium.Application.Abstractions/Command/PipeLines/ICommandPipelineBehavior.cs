using System.Threading;
using System.Threading.Tasks;

namespace Domium.Application.Abstractions.Command.PipeLines;

public interface ICommandPipelineBehavior<in TCommand>
    where TCommand : ICommand
{
    Task HandleAsync(
        TCommand command,
        CancellationToken cancellationToken,
        CommandHandlerDelegate next);
}

public interface ICommandPipelineBehavior<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(
        TCommand command,
        CancellationToken cancellationToken,
        CommandHandlerDelegate<TResult> next);
}
