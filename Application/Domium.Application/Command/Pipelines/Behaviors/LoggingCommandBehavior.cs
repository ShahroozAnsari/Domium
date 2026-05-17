using Domium.Application.Abstractions.Command;
using Domium.Application.Abstractions.Command.PipeLines;
using Microsoft.Extensions.Logging;

namespace Domium.Application.Command.Pipelines.Behaviors;

public sealed class LoggingCommandBehavior<TCommand>(ILogger<LoggingCommandBehavior<TCommand>> logger)
    : ICommandPipelineBehavior<TCommand>
    where TCommand : ICommand
{
    private readonly ILogger<LoggingCommandBehavior<TCommand>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task HandleAsync(
        TCommand command,
        CancellationToken cancellationToken,
        CommandHandlerDelegate next)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        if (next == null) throw new ArgumentNullException(nameof(next));

        var commandName = typeof(TCommand).Name;

        _logger.LogInformation("Executing command {CommandName}", commandName);

        try
        {
            await next().ConfigureAwait(false);
            _logger.LogInformation("Executed command {CommandName}", commandName);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Command {CommandName} failed", commandName);
            throw;
        }
    }
}
