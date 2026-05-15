using Domium.Application.Abstraction.Command;
using Domium.Application.Abstraction.Command.PipeLines;
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
        await _unitOfWork.BeginAsync(cancellationToken);

        try
        {
            await next();
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}