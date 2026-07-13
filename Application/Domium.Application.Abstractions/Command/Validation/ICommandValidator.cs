using System.Threading;
using System.Threading.Tasks;

namespace Domium.Application.Abstractions.Command.Validation;

public interface ICommandValidator<in TCommand>
    where TCommand : ICommand
{
    Task ValidateAsync(TCommand command, CancellationToken cancellationToken = default);
}
