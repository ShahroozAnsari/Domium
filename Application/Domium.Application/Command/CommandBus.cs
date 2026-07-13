using Domium.Application.Abstractions.Command;
using Domium.Application.Abstractions.Command.PipeLines;
using Microsoft.Extensions.DependencyInjection;

namespace Domium.Application.Command;

/// <summary>
/// Resolves the handler and pipeline behaviors for a command and executes them.
/// Cross-cutting concerns (observability, logging, validation, transactions, idempotency)
/// are pipeline behaviors — the bus itself only builds and invokes the chain.
/// </summary>
public sealed class CommandBus(IServiceProvider serviceProvider) : ICommandBus
{
    public Task ExecuteAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        var handler = serviceProvider.GetRequiredService<ICommandHandler<TCommand>>();
        var behaviors = serviceProvider.GetServices<ICommandPipelineBehavior<TCommand>>().Reverse().ToArray();

        CommandHandlerDelegate pipeline = () => handler.HandleAsync(command, cancellationToken);

        foreach (var behavior in behaviors)
        {
            var next = pipeline;
            pipeline = () => behavior.HandleAsync(command, cancellationToken, next);
        }

        return pipeline();
    }

    public Task<TResult> ExecuteAsync<TCommand, TResult>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        var handler = serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResult>>();
        var behaviors = serviceProvider
            .GetServices<ICommandPipelineBehavior<TCommand, TResult>>()
            .Reverse()
            .ToArray();

        CommandHandlerDelegate<TResult> pipeline = () => handler.HandleAsync(command, cancellationToken);

        foreach (var behavior in behaviors)
        {
            var next = pipeline;
            pipeline = () => behavior.HandleAsync(command, cancellationToken, next);
        }

        return pipeline();
    }
}
