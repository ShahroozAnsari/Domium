namespace Domium.Caching.Redis.Internal;

/// <summary>
/// Provides Redis key naming conventions for invalidation indexes.
/// </summary>
internal static class RedisInvalidationIndexKeys
{
    /// <summary>
    /// Creates a tag index key.
    /// </summary>
    /// <param name="tag">
    /// The tag value.
    /// </param>
    /// <returns>
    /// The Redis key.
    /// </returns>
    public static string Tag(string tag)
    {
        return $"domium:cache:index:tag:{tag}";
    }

    /// <summary>
    /// Creates an entity key index key.
    /// </summary>
    /// <param name="entityKey">
    /// The entity key value.
    /// </param>
    /// <returns>
    /// The Redis key.
    /// </returns>
    public static string EntityKey(string entityKey)
    {
        return $"domium:cache:index:entity:{entityKey}";
    }

    /// <summary>
    /// Creates a group index key.
    /// </summary>
    /// <param name="group">
    /// The group value.
    /// </param>
    /// <returns>
    /// The Redis key.
    /// </returns>
    public static string Group(string group)
    {
        return $"domium:cache:index:group:{group}";
    }
}