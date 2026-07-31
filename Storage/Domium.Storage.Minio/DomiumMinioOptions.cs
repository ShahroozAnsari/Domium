using System;

namespace Domium.Storage.Minio;

/// <summary>
/// Connection settings for the MinIO blob store. One bucket holds every tenant's blobs;
/// tenants are separated by key prefix, not by bucket, so adding a tenant needs no provisioning.
/// </summary>
public sealed class DomiumMinioOptions
{
    /// <summary>Host and port only, e.g. "localhost:9000" — no scheme.</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Access key (MinIO root user).</summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>Secret key (MinIO root password).</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Bucket every blob lands in. Must be a valid S3 bucket name.</summary>
    public string Bucket { get; set; } = "routewerk";

    /// <summary>Whether to talk TLS to the endpoint. False for the local MinIO container.</summary>
    public bool UseSsl { get; set; }

    /// <summary>Throws when any required setting is missing.</summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Endpoint)) throw new InvalidOperationException("MinIO endpoint is required.");
        if (string.IsNullOrWhiteSpace(AccessKey)) throw new InvalidOperationException("MinIO access key is required.");
        if (string.IsNullOrWhiteSpace(SecretKey)) throw new InvalidOperationException("MinIO secret key is required.");
        if (string.IsNullOrWhiteSpace(Bucket)) throw new InvalidOperationException("MinIO bucket is required.");
    }
}
