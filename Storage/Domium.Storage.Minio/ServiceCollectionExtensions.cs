using System;
using Domium.Storage.Abstractions;
using Domium.Tenancy.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Minio;

namespace Domium.Storage.Minio;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers the MinIO <see cref="IDomiumBlobStore"/> from explicit options.</summary>
    public static IServiceCollection AddDomiumMinioStorage(
        this IServiceCollection services,
        Action<DomiumMinioOptions> configure)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var options = new DomiumMinioOptions();
        configure(options);
        options.Validate();

        services.TryAddSingleton(options);
        services.TryAddSingleton<IMinioClient>(_ => new MinioClient()
            .WithEndpoint(options.Endpoint)
            .WithCredentials(options.AccessKey, options.SecretKey)
            .WithSSL(options.UseSsl)
            .Build());

        // Singleton: IDomiumTenantAccessor reads the ambient (AsyncLocal) tenant per call, so a
        // single instance still keys correctly per request — and the bucket check stays once-per-process.
        services.TryAddSingleton<IDomiumBlobStore>(provider => new MinioBlobStore(
            provider.GetRequiredService<IMinioClient>(),
            provider.GetRequiredService<DomiumMinioOptions>(),
            provider.GetRequiredService<IDomiumTenantAccessor>()));

        return services;
    }
}
