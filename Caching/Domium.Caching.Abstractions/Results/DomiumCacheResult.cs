namespace Domium.Caching.Abstractions.Results;

/// <summary>
/// Represents the result of a cache lookup.
/// </summary>
/// <typeparam name="T">
/// The cached value type.
/// </typeparam>
public sealed class DomiumCacheResult<T>
{
    private DomiumCacheResult(bool hasValue, T? value)
    {
        HasValue = hasValue;
        Value = value;
    }

    /// <summary>
    /// Gets a value indicating whether the cache contains a value.
    /// </summary>
    public bool HasValue { get; }

    /// <summary>
    /// Gets the cached value when <see cref="HasValue"/> is <c>true</c>.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Creates a cache hit result.
    /// </summary>
    /// <param name="value">
    /// The cached value.
    /// </param>
    /// <returns>
    /// A cache hit result.
    /// </returns>
    public static DomiumCacheResult<T> Hit(T? value)
    {
        return new DomiumCacheResult<T>(true, value);
    }

    /// <summary>
    /// Creates a cache miss result.
    /// </summary>
    /// <returns>
    /// A cache miss result.
    /// </returns>
    public static DomiumCacheResult<T> Miss()
    {
        return new DomiumCacheResult<T>(false, default);
    }
}
