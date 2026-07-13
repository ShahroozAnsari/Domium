using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Domium.Caching.Abstractions;

/// <summary>
/// Builds deterministic cache keys from query objects: type name plus a SHA-256 of the
/// query's JSON. Queries must therefore be fully serializable value objects (records with
/// plain properties) — members hidden from serialization are not part of the key.
/// </summary>
public static class DomiumCacheKeyBuilder
{
    public static string BuildQueryKey(object query, string prefix = "domium:query")
    {
        if (query == null) throw new ArgumentNullException(nameof(query));

        var type = query.GetType();
        var payload = JsonSerializer.Serialize(query, type);

        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));

        return $"{prefix}:{type.FullName}:{Convert.ToBase64String(hash)}";
    }
}
