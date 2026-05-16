using System;
using System.Collections.Generic;
using System.Linq;

namespace Domium.Caching.Abstractions.Models;

/// <summary>
/// Represents metadata used to invalidate cache entries.
/// </summary>
public sealed class DomiumCacheInvalidationMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomiumCacheInvalidationMetadata"/> class.
    /// </summary>
    /// <param name="tags">
    /// Logical tags used to group related cache entries.
    /// </param>
    /// <param name="entityKeys">
    /// Entity-specific keys used for precise invalidation.
    /// </param>
    /// <param name="group">
    /// An optional logical group name.
    /// </param>
    public DomiumCacheInvalidationMetadata(
        IEnumerable<string>? tags,
        IEnumerable<string>? entityKeys,
        string? group)
    {
        Tags = Normalize(tags);
        EntityKeys = Normalize(entityKeys);
        Group = string.IsNullOrWhiteSpace(group) ? null : group.Trim();
    }

    /// <summary>
    /// Gets the collection of tags associated with the cache entry.
    /// </summary>
    public IReadOnlyCollection<string> Tags { get; }

    /// <summary>
    /// Gets the collection of entity keys associated with the cache entry.
    /// </summary>
    public IReadOnlyCollection<string> EntityKeys { get; }

    /// <summary>
    /// Gets the logical group associated with the cache entry.
    /// </summary>
    public string? Group { get; }

    private static IReadOnlyCollection<string> Normalize(IEnumerable<string>? values)
    {
        if (values == null)
        {
            return Array.Empty<string>();
        }

        return values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}