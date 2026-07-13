using System;
using System.Collections.Generic;

namespace Domium.Caching.Abstractions;

/// <summary>How long an entry lives and which tags it can be invalidated by.</summary>
public sealed class DomiumCacheEntryOptions
{
    public DomiumCacheEntryOptions(TimeSpan duration, IReadOnlyCollection<string>? tags = null)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Cache duration must be greater than zero.");
        }

        Duration = duration;
        Tags = tags ?? Array.Empty<string>();
    }

    public TimeSpan Duration { get; }

    public IReadOnlyCollection<string> Tags { get; }
}
