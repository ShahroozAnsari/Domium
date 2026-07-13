using Domium.Application.Abstractions.Command;
using Domium.Application.Abstractions.Command.PipeLines;
using Domium.Persistence.Abstractions;

namespace Domium.Application.Command.Pipelines.Behaviors;

public sealed class TransactionCommandBehavior<TCommand>(IUnitOfWork unitOfWork) : ICommandPipelineBehavior<TCommand>
    where TCommand : ICommand
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    public async Task HandleAsync(
        TCommand command,
        CancellationToken cancellationToken,
        CommandHandlerDelegate next)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        if (next == null) throw new ArgumentNullException(nameof(next));

        await _unitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await next().ConfigureAwait(false);
            await _unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // The incoming token may already be cancelled; the compensating rollback must
            // still run, so it gets a token that cannot be cancelled.
            await _unitOfWork.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
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

        await _unitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var result = await next().ConfigureAwait(false);
            await _unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }
}
