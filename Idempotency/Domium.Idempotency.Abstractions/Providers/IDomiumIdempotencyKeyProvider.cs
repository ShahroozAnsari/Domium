using Domium.Application.Abstractions.Command;

namespace Domium.Idempotency.Abstractions.Providers;

public interface IDomiumIdempotencyKeyProvider
{
    string GetKey<TCommand>(
        TCommand command,
        string keyPrefix)
        where TCommand : ICommand;
}
