using Domium.Application.Abstractions.Command;
using Domium.Facade.Abstractions;

namespace Domium.Facade;

public abstract class DomiumCommandFacade(ICommandBus commandBus) : IFacade
{
    protected Task ExecuteAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        return commandBus.ExecuteAsync(command, cancellationToken);
    }


}
