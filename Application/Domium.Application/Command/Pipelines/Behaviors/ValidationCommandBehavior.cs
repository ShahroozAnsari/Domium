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
        if (command == null) throw new ArgumentNullException(nameof(command));
        if (next == null) throw new ArgumentNullException(nameof(next));

        foreach (var validator in validators)
        {
            await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        }

        await next().ConfigureAwait(false);
    }
}

public sealed class ValidationCommandBehavior<TCommand, TResult>(IEnumerable<ICommandValidator<TCommand>> validators)
    : ICommandPipelineBehavior<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public async Task<TResult> HandleAsync(
        TCommand command,
        CancellationToken cancellationToken,
        CommandHandlerDelegate<TResult> next)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        if (next == null) throw new ArgumentNullException(nameof(next));

        foreach (var validator in validators)
        {
            await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        }

        return await next().ConfigureAwait(false);
    }
}
