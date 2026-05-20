using System;

namespace Domium.Configuration;

public sealed class DomiumIdempotencyOptions
{
    public DomiumCacheStoreOptions Store { get; } = new();

    public TimeSpan Expiration { get; set; } = TimeSpan.FromHours(24);

    public string KeyPrefix { get; set; } = "domium";

    public bool RequireIdempotencyKey { get; set; }
}
