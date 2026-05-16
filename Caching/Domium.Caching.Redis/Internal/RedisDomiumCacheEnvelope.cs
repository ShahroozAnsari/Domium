using Domium.Caching.Abstractions.Models;

namespace Domium.Caching.Redis.Internal;

/// <summary>
/// Represents the serialized Redis cache envelope.
/// </summary>
/// <typeparam name="T">
/// The cached value type.
/// </typeparam>
internal sealed class RedisDomiumCacheEnvelope<T>
{
    /// <summary>
    /// Gets or sets a value indicating whether the entry exists in cache.
    /// </summary>
    public bool HasValue { get; set; }

    /// <summary>
    /// Gets or sets the cached value.
    /// </summary>
    public T? Value { get; set; }

    /// <summary>
    /// Gets or sets the invalidation metadata.
    /// </summary>
    public DomiumCacheInvalidationMetadata? Metadata { get; set; }
}