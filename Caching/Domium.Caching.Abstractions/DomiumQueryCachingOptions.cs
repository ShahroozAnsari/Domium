using System;

namespace Domium.Caching.Abstractions;

/// <summary>Defaults applied by the query-caching pipeline behavior.</summary>
public sealed class DomiumQueryCachingOptions
{
    public TimeSpan DefaultDuration { get; set; } = TimeSpan.FromMinutes(5);
}
