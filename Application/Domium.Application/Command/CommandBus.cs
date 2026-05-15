using Domium.Application.Abstraction.Command;
using Domium.Application.Abstraction.Command.PipeLines;
using Microsoft.Extensions.DependencyInjection;

namespace Domium.Application.Command;

public sealed class CommandBus(IServiceProvider serviceProvider) : ICommandBus
{
    public Task ExecuteAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
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
}