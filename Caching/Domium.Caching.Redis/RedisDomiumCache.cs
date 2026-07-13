using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Domium.Caching.Abstractions;
using StackExchange.Redis;

namespace Domium.Caching.Redis;

/// <summary>
/// Redis <see cref="IDomiumCache"/>. Values are stored as JSON with a TTL; tags are Redis
/// sets ("domium:cache:tag:{tag}") holding the member keys. Tag sets carry a TTL at least as
/// long as their newest member, so nothing leaks when entries expire naturally.
/// </summary>
public sealed class RedisDomiumCache(IConnectionMultiplexer connection) : IDomiumCache
{
    private const string TagKeyPrefix = "domium:cache:tag:";

    private readonly IConnectionMultiplexer _connection =
        connection ?? throw new ArgumentNullException(nameof(connection));

    private IDatabase Database => _connection.GetDatabase();

    public async Task<DomiumCacheResult<T>> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var payload = await Database.StringGetAsync(key).ConfigureAwait(false);

        return payload.IsNullOrEmpty
            ? DomiumCacheResult<T>.Miss()
            : DomiumCacheResult<T>.Hit(JsonSerializer.Deserialize<T>((string)payload!));
    }

    public async Task SetAsync<T>(string key, T value, DomiumCacheEntryOptions options, CancellationToken cancellationToken = default)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));

        var payload = JsonSerializer.Serialize(value);

        // Value + tag memberships are written in one transaction so a concurrent
        // RemoveByTagAsync can never observe the value without its tags.
        var transaction = Database.CreateTransaction();
        _ = transaction.StringSetAsync(key, payload, options.Duration);

        foreach (var tag in options.Tags)
        {
            var tagKey = TagKey(tag);
            _ = transaction.SetAddAsync(tagKey, key);
            _ = transaction.KeyExpireAsync(tagKey, options.Duration, ExpireWhen.GreaterThanCurrentExpiry);
        }

        await transaction.ExecuteAsync().ConfigureAwait(false);
    }

    public async Task<bool> TrySetAsync<T>(string key, T value, DomiumCacheEntryOptions options, CancellationToken cancellationToken = default)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));

        var payload = JsonSerializer.Serialize(value);

        // SET NX — atomic reservation.
        return await Database
            .StringSetAsync(key, payload, options.Duration, When.NotExists)
            .ConfigureAwait(false);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        Database.KeyDeleteAsync(key);

    public async Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        var tagKey = TagKey(tag);
        var members = await Database.SetMembersAsync(tagKey).ConfigureAwait(false);

        if (members.Length > 0)
        {
            var keys = members.Select(member => (RedisKey)member.ToString()).ToArray();
            await Database.KeyDeleteAsync(keys).ConfigureAwait(false);
        }

        await Database.KeyDeleteAsync(tagKey).ConfigureAwait(false);
    }

    private static string TagKey(string tag) => TagKeyPrefix + tag;
}
