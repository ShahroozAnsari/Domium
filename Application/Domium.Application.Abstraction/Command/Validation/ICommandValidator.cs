using System.Threading;
using System.Threading.Tasks;

namespace Domium.Application.Abstraction.Command.Validation;

public interface ICommandValidator<in TCommand>
{
    Task ValidateAsync(TCommand command, CancellationToken cancellationToken = default);
}