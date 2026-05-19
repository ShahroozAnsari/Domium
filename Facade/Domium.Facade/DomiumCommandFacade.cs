using Domium.Application.Abstractions.Command;
using Domium.Facade.Abstractions;

namespace Domium.Facade;

public sealed class DomiumCommandFacade(ICommandBus commandBus) : ICommandFacade
{
    public Task ExecuteAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        return commandBus.ExecuteAsync(command, cancellationToken);
    }
}
