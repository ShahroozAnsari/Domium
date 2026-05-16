using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Domium.Caching.Abstractions.Models;
using Domium.Caching.Abstractions.Providers;

namespace Domium.Caching.Providers;

/// <summary>
/// Creates deterministic cache keys for query instances.
/// </summary>
public sealed class DefaultDomiumCacheKeyProvider : IDomiumCacheKeyProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = false
    };

    /// <summary>
    /// Creates a deterministic cache key for the specified query and scope.
    /// </summary>
    /// <typeparam name="TQuery">
    /// The query type.
    /// </typeparam>
    /// <param name="query">
    /// The query instance.
    /// </param>
    /// <param name="policy">
    /// The cache policy.
    /// </param>
    /// <param name="scope">
    /// The resolved cache scope.
    /// </param>
    /// <returns>
    /// A deterministic cache key.
    /// </returns>
    public string CreateKey<TQuery>(
        TQuery query,
        DomiumQueryCachePolicy policy,
        DomiumCacheScope scope)
        where TQuery : class
    {
        if (query == null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        if (policy == null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        if (scope == null)
        {
            throw new ArgumentNullException(nameof(scope));
        }

        var queryType = typeof(TQuery);
        var queryTypeName = queryType.FullName ?? queryType.Name;
        var prefix = string.IsNullOrWhiteSpace(policy.KeyPrefix)
            ? "domium"
            : policy.KeyPrefix.Trim();

        var scopeSegment = scope.Kind == DomiumCacheScopeKind.Global
            ? "global"
            : $"tenant:{scope.TenantId}";

        var payload = JsonSerializer.Serialize(query, SerializerOptions);
        var hash = ComputeSha256(payload);

        return $"{prefix}:{queryTypeName}:{scopeSegment}:{hash}";
    }

    private static string ComputeSha256(string input)
    {
        using (var sha = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(input ?? string.Empty);
            var hashBytes = sha.ComputeHash(bytes);
            var builder = new StringBuilder(hashBytes.Length * 2);

            for (var i = 0; i < hashBytes.Length; i++)
            {
                builder.Append(hashBytes[i].ToString("X2"));
            }

            return builder.ToString();
        }
    }
}