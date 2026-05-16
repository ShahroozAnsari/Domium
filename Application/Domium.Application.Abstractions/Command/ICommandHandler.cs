using System.Threading;
using System.Threading.Tasks;

namespace Domium.Application.Abstractions.Command;

public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}