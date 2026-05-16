using System;

namespace Domium.Caching.Abstractions.Models
{
    /// <summary>
    /// Represents expiration settings for a cache entry.
    /// </summary>
    public sealed class DomiumCacheEntryOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DomiumCacheEntryOptions"/> class.
        /// </summary>
        /// <param name="absoluteExpirationRelativeToNow">
        /// The absolute expiration relative to now.
        /// </param>
        /// <param name="slidingExpiration">
        /// The sliding expiration interval.
        /// </param>
        public DomiumCacheEntryOptions(
            TimeSpan? absoluteExpirationRelativeToNow,
            TimeSpan? slidingExpiration)
        {
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

            AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow;
            SlidingExpiration = slidingExpiration;
        }

        /// <summary>
        /// Gets the absolute expiration relative to now.
        /// </summary>
        public TimeSpan? AbsoluteExpirationRelativeToNow { get; }

        /// <summary>
        /// Gets the sliding expiration interval.
        /// </summary>
        public TimeSpan? SlidingExpiration { get; }
    }
}