using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Domium.Storage.Abstractions;

/// <summary>
/// The single Domium blob abstraction: store a file, read it back, delete it, or hand the
/// browser a short-lived direct URL.
/// </summary>
/// <remarks>
/// Keys returned by <see cref="PutAsync"/> are already tenant-scoped — persist the key as-is
/// and pass it straight back to the other members. Implementations reject keys belonging to a
/// different tenant than the one currently in scope, so a leaked key from another tenant is
/// not readable.
/// </remarks>
public interface IDomiumBlobStore
{
    /// <summary>
    /// Stores <paramref name="content"/> and returns the key to persist against the aggregate.
    /// </summary>
    /// <param name="container">Logical grouping, e.g. "vehicle-photos". Lowercase, no slashes.</param>
    /// <param name="fileName">Original file name — only its extension is preserved.</param>
    /// <param name="content">The bytes to store.</param>
    /// <param name="contentType">MIME type to store alongside the blob.</param>
    /// <param name="cancellationToken">Cancels the upload.</param>
    Task<string> PutAsync(
        string container,
        string fileName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>Reads a blob back, or returns <c>null</c> when the key does not exist.</summary>
    Task<DomiumBlobContent?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Removes a blob. Succeeds when the key is already gone.</summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a time-limited URL the browser can fetch directly, so image bytes never flow
    /// through the API.
    /// </summary>
    Task<string> GetPresignedUrlAsync(
        string key,
        TimeSpan expiry,
        CancellationToken cancellationToken = default);
}
