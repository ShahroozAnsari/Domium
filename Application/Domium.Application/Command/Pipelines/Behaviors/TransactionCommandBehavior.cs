using Domium.Application.Abstractions.Command;
using Domium.Application.Abstractions.Command.PipeLines;
using Domium.Persistence.Abstractions;

namespace Domium.Application.Command.Pipelines.Behaviors;

/// <summary>
/// Wraps the rest of the pipeline in a unit of work. Delegating to
/// <see cref="IUnitOfWork.ExecuteAsync"/> keeps the transaction compatible with retrying
/// execution strategies and centralizes rollback semantics in the provider.
/// </summary>
public sealed class TransactionCommandBehavior<TCommand>(IUnitOfWork unitOfWork) : ICommandPipelineBehavior<TCommand>
    where TCommand : ICommand
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    public Task HandleAsync(
        TCommand command,
        CancellationToken cancellationToken,
        CommandHandlerDelegate next)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        if (next == null) throw new ArgumentNullException(nameof(next));

        return _unitOfWork.ExecuteAsync(() => next(), cancellationToken);
    }
}

public sealed class TransactionCommandBehavior<TCommand, TResult>(IUnitOfWork unitOfWork)
    : ICommandPipelineBehavior<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    public async Task<TResult> HandleAsync(
        TCommand command,
        CancellationToken cancellationToken,
        CommandHandlerDelegate<TResult> next)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        if (next == null) throw new ArgumentNullException(nameof(next));

        var result = default(TResult)!;

        await _unitOfWork.ExecuteAsync(
            async () => result = await next().ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);

        return result;
    }
}
