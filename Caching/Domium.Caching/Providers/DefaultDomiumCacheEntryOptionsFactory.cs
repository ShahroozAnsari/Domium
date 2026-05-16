using System;
using Domium.Caching.Abstractions.Models;
using Domium.Caching.Abstractions.Providers;

namespace Domium.Caching.Providers;

/// <summary>
/// Creates cache entry options from query cache policies.
/// </summary>
public sealed class DefaultDomiumCacheEntryOptionsFactory : IDomiumCacheEntryOptionsFactory
{
    private readonly TimeSpan? _defaultAbsoluteExpirationRelativeToNow;

    public DefaultDomiumCacheEntryOptionsFactory()
    {
    }

    public DefaultDomiumCacheEntryOptionsFactory(TimeSpan? defaultAbsoluteExpirationRelativeToNow)
    {
        if (defaultAbsoluteExpirationRelativeToNow.HasValue &&
            defaultAbsoluteExpirationRelativeToNow.Value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(defaultAbsoluteExpirationRelativeToNow),
                "Default expiration must be greater than zero.");
        }

        _defaultAbsoluteExpirationRelativeToNow = defaultAbsoluteExpirationRelativeToNow;
    }

    /// <summary>
    /// Creates cache entry options for the specified policy.
    /// </summary>
    /// <param name="policy">
    /// The query cache policy.
    /// </param>
    /// <returns>
    /// A <see cref="DomiumCacheEntryOptions"/> instance.
    /// </returns>
    public DomiumCacheEntryOptions Create(DomiumQueryCachePolicy policy)
    {
        if (policy == null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        return new DomiumCacheEntryOptions(
            policy.AbsoluteExpirationRelativeToNow ?? _defaultAbsoluteExpirationRelativeToNow,
            policy.SlidingExpiration);
    }
}