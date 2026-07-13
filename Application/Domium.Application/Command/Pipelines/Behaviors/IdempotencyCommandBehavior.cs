using Domium.Application.Abstractions.Command;
using Domium.Application.Abstractions.Command.PipeLines;
using Domium.Caching.Abstractions;
using Domium.Idempotency.Abstractions.Models;
using Domium.Idempotency.Abstractions.Providers;
using Microsoft.Extensions.Logging;

namespace Domium.Application.Command.Pipelines.Behaviors;

/// <summary>
/// Suppresses duplicate executions of <see cref="IIdempotentCommand"/>s. The reservation is
/// an atomic try-set in <see cref="IDomiumCache"/>; a duplicate of a completed command is a
/// silent no-op, while a duplicate of a still-running (or crashed mid-flight) command throws
/// so the caller can retry once the outcome is known.
/// </summary>
public sealed class IdempotencyCommandBehavior<TCommand>(
    IDomiumCache cache,
    IDomiumIdempotencyKeyProvider keyProvider,
    DomiumIdempotencyBehaviorOptions options,
    ILogger<IdempotencyCommandBehavior<TCommand>> logger)
    : ICommandPipelineBehavior<TCommand>
    where TCommand : ICommand
{
    private readonly IDomiumCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private readonly IDomiumIdempotencyKeyProvider _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
    private readonly DomiumIdempotencyBehaviorOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger<IdempotencyCommandBehavior<TCommand>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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

        // The memory store gives no cross-instance guarantee; a duplicate hitting another
        // instance will still execute. Detected by type name so the application layer does
        // not have to reference the memory provider package.
        if (_cache.GetType().Name == "MemoryDomiumCache" && IdempotencyStoreWarning.ShouldWarn())
        {
            _logger.LogWarning(
                "Idempotency is using the in-memory cache store, which only suppresses duplicates " +
                "within this process. Use a distributed store (e.g. Redis) when running more than one instance.");
        }

        var key = _keyProvider.GetKey(command, _options.KeyPrefix);
        var commandName = typeof(TCommand).FullName ?? typeof(TCommand).Name;
        var entryOptions = new DomiumCacheEntryOptions(_options.Expiration);
        var entry = new DomiumIdempotencyEntry(
            key,
            commandName,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.Add(_options.Expiration));

        var reserved = await _cache.TrySetAsync(key, entry, entryOptions, cancellationToken).ConfigureAwait(false);

        if (!reserved)
        {
            var existing = await _cache.GetAsync<DomiumIdempotencyEntry>(key, cancellationToken).ConfigureAwait(false);

            if (existing.Found && existing.Value?.CompletedAt is null)
            {
                throw new InvalidOperationException(
                    $"Command {commandName} with idempotency key '{key}' is already in progress or its outcome is unknown.");
            }

            return;
        }

        try
        {
            await next().ConfigureAwait(false);
        }
        catch
        {
            // Release the reservation even when the caller's token is already cancelled.
            await _cache.RemoveAsync(key, CancellationToken.None).ConfigureAwait(false);
            throw;
        }

        var completedEntry = new DomiumIdempotencyEntry(
            entry.Key,
            entry.CommandName,
            entry.CreatedAt,
            entry.ExpiresAt,
            DateTimeOffset.UtcNow);

        await _cache.SetAsync(key, completedEntry, entryOptions, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Process-wide once-only latch for the non-distributed-store warning.</summary>
internal static class IdempotencyStoreWarning
{
    private static int _warned;

    public static bool ShouldWarn() => Interlocked.Exchange(ref _warned, 1) == 0;
}
