using System;
using System.Threading;
using System.Threading.Tasks;
using Domium.Caching.Abstractions.Models;
using Domium.Caching.Abstractions.Results;
using Domium.Caching.Abstractions.Stores;

namespace Domium.Caching.Stores;

public abstract class DomiumCacheStoreAdapter(IDomiumCacheStore inner) : IDomiumCacheStore, IDisposable
{
    private readonly IDomiumCacheStore _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    public Task<DomiumCacheResult<T>> TryGetAsync<T>(
        string key,
        CancellationToken cancellationToken)
    {
        return _inner.TryGetAsync<T>(key, cancellationToken);
    }

    public Task SetAsync<T>(
        string key,
        T value,
        DomiumCacheEntryOptions options,
        DomiumCacheInvalidationMetadata invalidationMetadata,
        CancellationToken cancellationToken)
    {
        return _inner.SetAsync(key, value, options, invalidationMetadata, cancellationToken);
    }

    public Task<bool> TrySetAsync<T>(
        string key,
        T value,
        DomiumCacheEntryOptions options,
        DomiumCacheInvalidationMetadata invalidationMetadata,
        CancellationToken cancellationToken)
    {
        return _inner.TrySetAsync(key, value, options, invalidationMetadata, cancellationToken);
    }

    public Task RemoveAsync(
        string key,
        CancellationToken cancellationToken)
    {
        return _inner.RemoveAsync(key, cancellationToken);
    }

    public Task RemoveByTagAsync(
        string tag,
        CancellationToken cancellationToken)
    {
        return _inner.RemoveByTagAsync(tag, cancellationToken);
    }

    public Task RemoveByEntityKeyAsync(
        string entityKey,
        CancellationToken cancellationToken)
    {
        return _inner.RemoveByEntityKeyAsync(entityKey, cancellationToken);
    }

    public Task RemoveByGroupAsync(
        string group,
        CancellationToken cancellationToken)
    {
        return _inner.RemoveByGroupAsync(group, cancellationToken);
    }

    public void Dispose()
    {
        if (_inner is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
