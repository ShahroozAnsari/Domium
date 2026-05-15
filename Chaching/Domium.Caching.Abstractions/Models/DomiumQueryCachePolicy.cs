using System;

namespace Domium.Caching.Abstractions.Models
{
    /// <summary>
    /// Represents the cache policy for a query type.
    /// </summary>
    public sealed class DomiumQueryCachePolicy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DomiumQueryCachePolicy"/> class.
        /// </summary>
        /// <param name="queryType">
        /// The query CLR type.
        /// </param>
        /// <param name="enabled">
        /// Indicates whether caching is enabled.
        /// </param>
        /// <param name="scopeMode">
        /// The scope mode to use for cache key generation.
        /// </param>
        /// <param name="absoluteExpirationRelativeToNow">
        /// The absolute expiration relative to now.
        /// </param>
        /// <param name="slidingExpiration">
        /// The sliding expiration interval.
        /// </param>
        /// <param name="keyPrefix">
        /// An optional key prefix.
        /// </param>
        /// <param name="cacheNullValues">
        /// Indicates whether null results should be cached.
        /// </param>
        /// <param name="invalidationMetadata">
        /// Static invalidation metadata associated with the query type.
        /// </param>
        public DomiumQueryCachePolicy(
            Type queryType,
            bool enabled,
            DomiumQueryCacheScopeMode scopeMode,
            TimeSpan? absoluteExpirationRelativeToNow,
            TimeSpan? slidingExpiration,
            string keyPrefix,
            bool cacheNullValues,
            DomiumCacheInvalidationMetadata invalidationMetadata)
        {
            if (queryType == null)
            {
                throw new ArgumentNullException(nameof(queryType));
            }

            if (absoluteExpirationRelativeToNow.HasValue && absoluteExpirationRelativeToNow.Value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(absoluteExpirationRelativeToNow),
                    "Absolute expiration must be greater than zero.");
            }

            if (slidingExpiration.HasValue && slidingExpiration.Value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(slidingExpiration),
                    "Sliding expiration must be greater than zero.");
            }

            if (keyPrefix != null && keyPrefix.Length == 0)
            {
                throw new ArgumentException("Key prefix cannot be empty.", nameof(keyPrefix));
            }

            QueryType = queryType;
            Enabled = enabled;
            ScopeMode = scopeMode;
            AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow;
            SlidingExpiration = slidingExpiration;
            KeyPrefix = keyPrefix;
            CacheNullValues = cacheNullValues;
            InvalidationMetadata = invalidationMetadata;
        }

        /// <summary>
        /// Gets the query CLR type.
        /// </summary>
        public Type QueryType { get; }

        /// <summary>
        /// Gets a value indicating whether caching is enabled.
        /// </summary>
        public bool Enabled { get; }

        /// <summary>
        /// Gets the configured cache scope mode.
        /// </summary>
        public DomiumQueryCacheScopeMode ScopeMode { get; }

        /// <summary>
        /// Gets the absolute expiration relative to now.
        /// </summary>
        public TimeSpan? AbsoluteExpirationRelativeToNow { get; }

        /// <summary>
        /// Gets the sliding expiration interval.
        /// </summary>
        public TimeSpan? SlidingExpiration { get; }

        /// <summary>
        /// Gets the key prefix used during cache key generation.
        /// </summary>
        public string KeyPrefix { get; }

        /// <summary>
        /// Gets a value indicating whether null results should be cached.
        /// </summary>
        public bool CacheNullValues { get; }

        /// <summary>
        /// Gets the static invalidation metadata for the query type.
        /// </summary>
        public DomiumCacheInvalidationMetadata InvalidationMetadata { get; }
    }
}
