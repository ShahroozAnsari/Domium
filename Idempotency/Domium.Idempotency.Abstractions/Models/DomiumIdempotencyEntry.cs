using System;

namespace Domium.Idempotency.Abstractions.Models;

public sealed class DomiumIdempotencyEntry(
    string key,
    string commandName,
    DateTimeOffset createdAt,
    DateTimeOffset expiresAt,
    DateTimeOffset? completedAt = null)
{
    public string Key { get; } = key;

    public string CommandName { get; } = commandName;

    public DateTimeOffset CreatedAt { get; } = createdAt;

    public DateTimeOffset ExpiresAt { get; } = expiresAt;

    public DateTimeOffset? CompletedAt { get; } = completedAt;
}
