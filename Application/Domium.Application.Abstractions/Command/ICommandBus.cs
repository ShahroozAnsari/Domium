using System.Threading;
using System.Threading.Tasks;

namespace Domium.Application.Abstractions.Command;

public interface ICommandBus
{
    Task ExecuteAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand;

    Task<TResult> ExecuteAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>;
}
