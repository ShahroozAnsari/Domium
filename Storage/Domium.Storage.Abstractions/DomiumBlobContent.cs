using System;
using System.IO;

namespace Domium.Storage.Abstractions;

/// <summary>
/// A blob read back out of the store. Owns <see cref="Content"/> — dispose it to release
/// the underlying network stream.
/// </summary>
public sealed class DomiumBlobContent : IDisposable
{
    /// <summary>Creates a blob payload over an already-positioned stream.</summary>
    public DomiumBlobContent(Stream content, string contentType, long length)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        ContentType = contentType;
        Length = length;
    }

    /// <summary>The blob bytes, positioned at the start.</summary>
    public Stream Content { get; }

    /// <summary>MIME type the blob was stored with.</summary>
    public string ContentType { get; }

    /// <summary>Size in bytes.</summary>
    public long Length { get; }

    /// <inheritdoc />
    public void Dispose() => Content.Dispose();
}
