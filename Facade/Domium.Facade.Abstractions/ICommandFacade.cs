using System.Threading;
using System.Threading.Tasks;
using Domium.Application.Abstractions.Command;

namespace Domium.Facade.Abstractions;

public interface ICommandFacade
{
    Task ExecuteAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand;
}
