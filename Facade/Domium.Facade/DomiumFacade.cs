using Domium.Application.Abstractions.Command;
using Domium.Application.Abstractions.Query;
using Domium.Facade.Abstractions;

namespace Domium.Facade;

public abstract class DomiumFacade(ICommandBus commandBus, IQueryBus queryBus) : IFacade
{
    protected Task ExecuteAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        return commandBus.ExecuteAsync(command, cancellationToken);
    }

    protected Task<TResult> QueryAsync<TQuery, TResult>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : class, IQuery<TResult>
        where TResult : class
    {
        return queryBus.ExecuteAsync<TQuery, TResult>(query, cancellationToken);
    }
}
