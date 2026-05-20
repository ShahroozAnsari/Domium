using System;

namespace Domium.Idempotency.Abstractions.Models;

public sealed class DomiumIdempotencyEntry
{
    public DomiumIdempotencyEntry(
        string key,
        string commandName,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        DateTimeOffset? completedAt = null)
    {
        Key = key;
        CommandName = commandName;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        CompletedAt = completedAt;
    }

    public string Key { get; }

    public string CommandName { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset ExpiresAt { get; }

    public DateTimeOffset? CompletedAt { get; }
}
