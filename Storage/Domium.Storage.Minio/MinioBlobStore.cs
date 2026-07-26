using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Domium.Storage.Abstractions;
using Domium.Tenancy.Abstractions;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace Domium.Storage.Minio;

/// <summary>
/// MinIO-backed <see cref="IDomiumBlobStore"/>. Keys are <c>{tenant}/{container}/{id}{ext}</c>,
/// which is what keeps one tenant's blobs unreadable from another's scope.
/// </summary>
public sealed class MinioBlobStore : IDomiumBlobStore
{
    private readonly IMinioClient _client;
    private readonly DomiumMinioOptions _options;
    private readonly IDomiumTenantAccessor _tenantAccessor;
    private readonly SemaphoreSlim _bucketGate = new(1, 1);
    private bool _bucketReady;

    /// <summary>Creates the store over a configured MinIO client.</summary>
    public MinioBlobStore(IMinioClient client, DomiumMinioOptions options, IDomiumTenantAccessor tenantAccessor)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _tenantAccessor = tenantAccessor ?? throw new ArgumentNullException(nameof(tenantAccessor));
    }

    /// <inheritdoc />
    public async Task<string> PutAsync(
        string container,
        string fileName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (content == null) throw new ArgumentNullException(nameof(content));

        var key = BuildKey(container, fileName);
        await EnsureBucketAsync(cancellationToken).ConfigureAwait(false);

        // MinIO needs the length up front; a non-seekable upload stream is buffered first.
        var payload = content;
        var buffered = false;
        if (!content.CanSeek)
        {
            payload = new MemoryStream();
            await content.CopyToAsync(payload, 81920, cancellationToken).ConfigureAwait(false);
            payload.Position = 0;
            buffered = true;
        }

        try
        {
            await _client.PutObjectAsync(
                new PutObjectArgs()
                    .WithBucket(_options.Bucket)
                    .WithObject(key)
                    .WithStreamData(payload)
                    .WithObjectSize(payload.Length - payload.Position)
                    .WithContentType(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType),
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (buffered) payload.Dispose();
        }

        return key;
    }

    /// <inheritdoc />
    public async Task<DomiumBlobContent?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        EnsureKeyBelongsToTenant(key);
        await EnsureBucketAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var stat = await _client.StatObjectAsync(
                new StatObjectArgs().WithBucket(_options.Bucket).WithObject(key),
                cancellationToken).ConfigureAwait(false);

            var buffer = new MemoryStream();
            await _client.GetObjectAsync(
                new GetObjectArgs()
                    .WithBucket(_options.Bucket)
                    .WithObject(key)
                    .WithCallbackStream((stream, token) => stream.CopyToAsync(buffer, 81920, token)),
                cancellationToken).ConfigureAwait(false);

            buffer.Position = 0;
            return new DomiumBlobContent(buffer, stat.ContentType ?? "application/octet-stream", stat.Size);
        }
        catch (ObjectNotFoundException)
        {
            return null;
        }
        catch (BucketNotFoundException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        EnsureKeyBelongsToTenant(key);

        try
        {
            await _client.RemoveObjectAsync(
                new RemoveObjectArgs().WithBucket(_options.Bucket).WithObject(key),
                cancellationToken).ConfigureAwait(false);
        }
        catch (ObjectNotFoundException)
        {
            // Deleting something already gone is the outcome the caller wanted.
        }
        catch (BucketNotFoundException)
        {
        }
    }

    /// <inheritdoc />
    public async Task<string> GetPresignedUrlAsync(
        string key,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        EnsureKeyBelongsToTenant(key);
        await EnsureBucketAsync(cancellationToken).ConfigureAwait(false);

        var seconds = (int)Math.Clamp(expiry.TotalSeconds, 1d, 7 * 24 * 60 * 60d);
        return await _client.PresignedGetObjectAsync(
            new PresignedGetObjectArgs()
                .WithBucket(_options.Bucket)
                .WithObject(key)
                .WithExpiry(seconds)).ConfigureAwait(false);
    }

    private string BuildKey(string container, string fileName)
    {
        if (string.IsNullOrWhiteSpace(container)) throw new ArgumentException("Container is required.", nameof(container));

        var extension = Path.GetExtension(fileName ?? string.Empty);
        if (extension.Length > 16) extension = string.Empty;

        return $"{CurrentTenantPrefix()}{Slug(container)}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
    }

    private void EnsureKeyBelongsToTenant(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Blob key is required.", nameof(key));

        var prefix = CurrentTenantPrefix();
        if (!key.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Blob key does not belong to the tenant in scope.");
        }
    }

    /// <summary>Tenant prefix including the trailing slash, or "shared/" when no tenant is in scope.</summary>
    private string CurrentTenantPrefix()
    {
        var tenant = _tenantAccessor.GetCurrent();
        return tenant is { IsAvailable: true, TenantId: { } id } ? $"{Slug(id)}/" : "shared/";
    }

    private async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        if (_bucketReady) return;

        await _bucketGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_bucketReady) return;

            var exists = await _client.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_options.Bucket), cancellationToken).ConfigureAwait(false);

            if (!exists)
            {
                await _client.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(_options.Bucket), cancellationToken).ConfigureAwait(false);
            }

            _bucketReady = true;
        }
        finally
        {
            _bucketGate.Release();
        }
    }

    /// <summary>Reduces a tenant or container name to the lowercase key-safe subset.</summary>
    private static string Slug(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character)) builder.Append(character);
            else if (character is '-' or '_' or '.') builder.Append(character);
            else if (character is ' ') builder.Append('-');
        }

        var slug = builder.ToString().Trim('-', '.');
        return slug.Length == 0 ? "default" : slug;
    }
}
