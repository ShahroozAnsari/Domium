using System;
using Domium.Application.Abstractions.Command;
using Domium.Idempotency.Abstractions.Providers;

namespace Domium.Idempotency.Providers;

public sealed class DefaultDomiumIdempotencyKeyProvider : IDomiumIdempotencyKeyProvider
{
    public string GetKey<TCommand>(
        TCommand command,
        string keyPrefix)
        where TCommand : ICommand
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        if (command is not IIdempotentCommand idempotentCommand)
        {
            throw new InvalidOperationException(
                $"Command {typeof(TCommand).FullName ?? typeof(TCommand).Name} does not implement {nameof(IIdempotentCommand)}.");
        }

        if (string.IsNullOrWhiteSpace(idempotentCommand.IdempotencyKey))
        {
            throw new InvalidOperationException(
                $"Command {typeof(TCommand).FullName ?? typeof(TCommand).Name} requires a non-empty idempotency key.");
        }

        var commandName = typeof(TCommand).FullName ?? typeof(TCommand).Name;
        return $"{keyPrefix}:idempotency:{commandName}:{idempotentCommand.IdempotencyKey.Trim()}";
    }
}
