using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Domium.Caching.Abstractions.Models;
using Domium.Caching.Abstractions.Providers;

namespace Domium.Caching.Providers;

public sealed class DefaultDomiumCacheKeyFactory : IDomiumCacheKeyFactory
{
    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = false
    };

    public string CreateKey(DomiumCacheKeyDescriptor descriptor)
    {
        if (descriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        var prefix = Normalize(descriptor.Prefix, "domium");
        var category = Normalize(descriptor.Category, "cache");
        var name = Normalize(descriptor.Name, "unknown");
        var scope = Normalize(descriptor.Scope, "global");
        var payload = JsonSerializer.Serialize(descriptor.Payload, SerializerOptions);
        var hash = ComputeSha256(payload);

        return $"{prefix}:{category}:{name}:{scope}:{hash}";
    }

    private static string Normalize(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim();
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
