using System.Threading;
using System.Threading.Tasks;

namespace Domium.Caching.Abstractions;

/// <summary>
/// The single Domium cache abstraction: get/set values with an absolute duration, optionally
/// labelled with tags, and invalidate everything carrying a tag in one call.
/// </summary>
public interface IDomiumCache
{
    /// <summary>Returns the cached value for <paramref name="key"/>, or a miss.</summary>
    Task<DomiumCacheResult<T>> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>Stores <paramref name="value"/> under <paramref name="key"/>.</summary>
    Task SetAsync<T>(string key, T value, DomiumCacheEntryOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores the value only when the key does not exist yet. Atomic per store — used for
    /// idempotency reservations.
    /// </summary>
    Task<bool> TrySetAsync<T>(string key, T value, DomiumCacheEntryOptions options, CancellationToken cancellationToken = default);

    /// <summary>Removes a single entry.</summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Removes every entry that was stored with <paramref name="tag"/>.</summary>
    Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default);
}
