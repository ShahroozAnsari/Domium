using Domium.Application.Abstractions.Command;
using Domium.Application.Abstractions.Query;
using Domium.Facade.Abstractions;

namespace Domium.Facade;

/// <summary>
/// Combined facade base for modules that expose both commands and queries through one API.
/// Use <see cref="DomiumCommandFacade"/> / <see cref="DomiumQueryFacade"/> when a facade
/// should stay write-only or read-only.
/// </summary>
public abstract class DomiumFacade(ICommandBus commandBus, IQueryBus queryBus) : IFacade
{
    protected Task ExecuteAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        return commandBus.ExecuteAsync(command, cancellationToken);
    }

    protected Task<TResult> ExecuteAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>
    {
        return commandBus.ExecuteAsync<TCommand, TResult>(command, cancellationToken);
    }

    protected Task<TResult> QueryAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>
    {
        return queryBus.ExecuteAsync<TQuery, TResult>(query, cancellationToken);
    }
}
