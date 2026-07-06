using Domium.Application.Abstractions.Command;
using Domium.Application.Abstractions.Command.PipeLines;
using Domium.Caching.Abstractions.Models;
using Domium.Caching.Abstractions.Stores;
using Domium.Idempotency.Abstractions.Models;
using Domium.Idempotency.Abstractions.Providers;

namespace Domium.Application.Command.Pipelines.Behaviors;

public sealed class IdempotencyCommandBehavior<TCommand>(
    IDomiumIdempotencyCacheStore cacheStore,
    IDomiumIdempotencyKeyProvider keyProvider,
    DomiumIdempotencyBehaviorOptions options)
    : ICommandPipelineBehavior<TCommand>
    where TCommand : ICommand
{
    private readonly IDomiumIdempotencyCacheStore _cacheStore =
        cacheStore ?? throw new ArgumentNullException(nameof(cacheStore));

    private readonly IDomiumIdempotencyKeyProvider _keyProvider =
        keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));

    private readonly DomiumIdempotencyBehaviorOptions _options =
        options ?? throw new ArgumentNullException(nameof(options));

    public async Task HandleAsync(
        TCommand command,
        CancellationToken cancellationToken,
        CommandHandlerDelegate next)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        if (next == null) throw new ArgumentNullException(nameof(next));

        if (command is not IIdempotentCommand)
        {
            if (_options.RequireIdempotencyKey)
            {
                throw new InvalidOperationException(
                    $"Command {typeof(TCommand).FullName ?? typeof(TCommand).Name} must implement {nameof(IIdempotentCommand)} when idempotency is required.");
            }

            await next().ConfigureAwait(false);
            return;
        }

        var key = _keyProvider.GetKey(command, _options.KeyPrefix);
        var commandName = typeof(TCommand).FullName ?? typeof(TCommand).Name;
        var cacheOptions = new DomiumCacheEntryOptions(_options.Expiration, null);
        var metadata = new DomiumCacheInvalidationMetadata(null, null, "idempotency");
        var entry = new DomiumIdempotencyEntry(
            key,
            commandName,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.Add(_options.Expiration));

        var reserved = await _cacheStore
            .TrySetAsync(key, entry, cacheOptions, metadata, cancellationToken)
            .ConfigureAwait(false);

        if (!reserved)
        {
            return;
        }

        try
        {
            await next().ConfigureAwait(false);
        }
        catch
        {
            await _cacheStore.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
            throw;
        }

        var completedEntry = new DomiumIdempotencyEntry(
            entry.Key,
            entry.CommandName,
            entry.CreatedAt,
            entry.ExpiresAt,
            DateTimeOffset.UtcNow);

        await _cacheStore
            .SetAsync(key, completedEntry, cacheOptions, metadata, cancellationToken)
            .ConfigureAwait(false);
    }
}
