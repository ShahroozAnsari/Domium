using System;

namespace Domium.Idempotency.Abstractions.Models;

public sealed class DomiumIdempotencyBehaviorOptions
{
    public TimeSpan Expiration { get; set; } = TimeSpan.FromHours(24);

    public string KeyPrefix { get; set; } = "domium";

    public bool RequireIdempotencyKey { get; set; }
}
