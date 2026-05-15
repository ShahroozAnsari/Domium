using Domium.Caching.Abstractions.Models;

namespace Domium.Caching.Memory.Internal
{
    /// <summary>
    /// Represents a memory cache envelope that stores the cached value and related metadata.
    /// </summary>
    /// <typeparam name="T">
    /// The cached value type.
    /// </typeparam>
    internal sealed class MemoryDomiumCacheEnvelope<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryDomiumCacheEnvelope{T}"/> class.
        /// </summary>
        /// <param name="hasValue">
        /// Indicates whether the cache represents a hit.
        /// </param>
        /// <param name="value">
        /// The cached value.
        /// </param>
        /// <param name="metadata">
        /// The invalidation metadata.
        /// </param>
        public MemoryDomiumCacheEnvelope(
            bool hasValue,
            T value,
            DomiumCacheInvalidationMetadata metadata)
        {
            HasValue = hasValue;
            Value = value;
            Metadata = metadata;
        }

        /// <summary>
        /// Gets a value indicating whether the envelope contains a cached entry.
        /// </summary>
        public bool HasValue { get; }

        /// <summary>
        /// Gets the cached value.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Gets the invalidation metadata.
        /// </summary>
        public DomiumCacheInvalidationMetadata Metadata { get; }
    }
}