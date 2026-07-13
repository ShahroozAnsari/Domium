using System;
using System.Collections.Generic;

namespace Domium.Caching.Abstractions;

/// <summary>
/// Opt-in marker for query caching: implement this on a query record and the caching
/// pipeline behavior caches its result. Override <see cref="Duration"/> to deviate from the
/// configured default, and supply <see cref="Tags"/> so command handlers can invalidate the
/// entry with <see cref="IDomiumCache.RemoveByTagAsync"/>.
/// </summary>
public interface ICacheableQuery
{
    /// <summary>Time to live; <c>null</c> uses the configured default duration.</summary>
    TimeSpan? Duration => null;

    /// <summary>Invalidation tags for this query's cache entry.</summary>
    IReadOnlyCollection<string> Tags => Array.Empty<string>();
}
