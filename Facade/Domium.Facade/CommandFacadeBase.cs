using Domium.Application.Abstractions.Command;
using Domium.Facade.Abstractions;

namespace Domium.Facade;

public abstract class CommandFacadeBase(ICommandFacade facade)
{
    protected Task ExecuteAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        return facade.ExecuteAsync(command, cancellationToken);
    }
}
