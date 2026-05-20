using System;
using Domium.Application.Abstractions.Command;
using Domium.Caching.Abstractions.Models;
using Domium.Caching.Abstractions.Providers;
using Domium.Idempotency.Abstractions.Providers;

namespace Domium.Idempotency.Providers;

public sealed class DefaultDomiumIdempotencyKeyProvider(IDomiumCacheKeyFactory keyFactory)
    : IDomiumIdempotencyKeyProvider
{
    private readonly IDomiumCacheKeyFactory _keyFactory =
        keyFactory ?? throw new ArgumentNullException(nameof(keyFactory));

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

        return _keyFactory.CreateKey(
            new DomiumCacheKeyDescriptor(
                keyPrefix,
                "idempotency",
                typeof(TCommand).FullName ?? typeof(TCommand).Name,
                "global",
                idempotentCommand.IdempotencyKey.Trim()));
    }
}
