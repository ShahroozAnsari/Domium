using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Domium.Caching.Memory.Internal
{
    /// <summary>
    /// Maintains reverse indexes from invalidation tokens to cache keys for in-memory cache invalidation.
    /// </summary>
    internal sealed class MemoryCacheIndex
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _tags;
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _entityKeys;
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _groups;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryCacheIndex"/> class.
        /// </summary>
        public MemoryCacheIndex()
        {
            _tags = new ConcurrentDictionary<string, ConcurrentDictionary<string, byte>>(StringComparer.Ordinal);
            _entityKeys = new ConcurrentDictionary<string, ConcurrentDictionary<string, byte>>(StringComparer.Ordinal);
            _groups = new ConcurrentDictionary<string, ConcurrentDictionary<string, byte>>(StringComparer.Ordinal);
        }

        /// <summary>
        /// Adds a key to the specified tag indexes.
        /// </summary>
        /// <param name="key">
        /// The cache key.
        /// </param>
        /// <param name="tags">
        /// The tags associated with the key.
        /// </param>
        public void AddTags(string key, IEnumerable<string> tags)
        {
            AddMany(_tags, key, tags);
        }

        /// <summary>
        /// Adds a key to the specified entity key indexes.
        /// </summary>
        /// <param name="key">
        /// The cache key.
        /// </param>
        /// <param name="entityKeys">
        /// The entity keys associated with the key.
        /// </param>
        public void AddEntityKeys(string key, IEnumerable<string> entityKeys)
        {
            AddMany(_entityKeys, key, entityKeys);
        }

        /// <summary>
        /// Adds a key to the specified group index.
        /// </summary>
        /// <param name="key">
        /// The cache key.
        /// </param>
        /// <param name="group">
        /// The group associated with the key.
        /// </param>
        public void AddGroup(string key, string? group)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                return;
            }

            var bucket = _groups.GetOrAdd(group, _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal));
            bucket[key] = 0;
        }

        /// <summary>
        /// Gets all keys associated with the specified tag.
        /// </summary>
        /// <param name="tag">
        /// The tag.
        /// </param>
        /// <returns>
        /// A collection of associated cache keys.
        /// </returns>
        public IReadOnlyCollection<string> GetKeysByTag(string tag)
        {
            return GetKeys(_tags, tag);
        }

        /// <summary>
        /// Removes a cache key from all reverse indexes.
        /// </summary>
        /// <param name="key">
        /// The cache key.
        /// </param>
        public void RemoveKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            RemoveKeyFrom(_tags, key);
            RemoveKeyFrom(_entityKeys, key);
            RemoveKeyFrom(_groups, key);
        }

        /// <summary>
        /// Gets all keys associated with the specified entity key.
        /// </summary>
        /// <param name="entityKey">
        /// The entity key.
        /// </param>
        /// <returns>
        /// A collection of associated cache keys.
        /// </returns>
        public IReadOnlyCollection<string> GetKeysByEntityKey(string entityKey)
        {
            return GetKeys(_entityKeys, entityKey);
        }

        /// <summary>
        /// Gets all keys associated with the specified group.
        /// </summary>
        /// <param name="group">
        /// The group.
        /// </param>
        /// <returns>
        /// A collection of associated cache keys.
        /// </returns>
        public IReadOnlyCollection<string> GetKeysByGroup(string group)
        {
            return GetKeys(_groups, group);
        }

        public IReadOnlyCollection<string> RemoveTag(string tag)
        {
            return RemoveBucket(_tags, tag);
        }

        public IReadOnlyCollection<string> RemoveEntityKey(string entityKey)
        {
            return RemoveBucket(_entityKeys, entityKey);
        }

        public IReadOnlyCollection<string> RemoveGroup(string group)
        {
            return RemoveBucket(_groups, group);
        }

        private static void AddMany(
            ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> source,
            string key,
            IEnumerable<string> values)
        {
            if (string.IsNullOrWhiteSpace(key) || values == null)
            {
                return;
            }

            foreach (var value in values.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                var normalized = value.Trim();
                var bucket = source.GetOrAdd(normalized, _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal));
                bucket[key] = 0;
            }
        }

        private static IReadOnlyCollection<string> GetKeys(
            ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> source,
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Array.Empty<string>();
            }

            if (!source.TryGetValue(value.Trim(), out var bucket))
            {
                return Array.Empty<string>();
            }

            return bucket.Keys.ToArray();
        }

        private static IReadOnlyCollection<string> RemoveBucket(
            ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> source,
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Array.Empty<string>();
            }

            return source.TryRemove(value.Trim(), out var bucket)
                ? bucket.Keys.ToArray()
                : Array.Empty<string>();
        }

        private static void RemoveKeyFrom(
            ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> source,
            string key)
        {
            foreach (var bucket in source)
            {
                bucket.Value.TryRemove(key, out _);

                if (bucket.Value.IsEmpty)
                {
                    source.TryRemove(bucket.Key, out _);
                }
            }
        }
    }
}
