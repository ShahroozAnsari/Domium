using Domium.Application.Abstractions.Command;
using Domium.Application.Abstractions.Command.PipeLines;
using Domium.Application.Abstractions.Command.Validation;

namespace Domium.Application.Command.Pipelines.Behaviors;

public sealed class ValidationCommandBehavior<TCommand>(IEnumerable<ICommandValidator<TCommand>> validators)
    : ICommandPipelineBehavior<TCommand>
    where TCommand : ICommand
{
    public async Task HandleAsync(
        TCommand command,
        CancellationToken cancellationToken,
        CommandHandlerDelegate next)
    {
        foreach (var validator in validators)
        {
            await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        }

        await next().ConfigureAwait(false);
    }
}


