using Domium.Configuration;
using Domium.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Domium.Observability.OpenTelemetry;

/// <summary>
/// Dependency injection helpers for Domium OpenTelemetry integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDomiumOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        var section = configuration.GetSection(DomiumOpenTelemetryOptions.SectionName);
        services.Configure<DomiumOpenTelemetryOptions>(section);

        var options = section.Get<DomiumOpenTelemetryOptions>() ?? new DomiumOpenTelemetryOptions();
        return services.AddDomiumOpenTelemetry(options);
    }

    public static IServiceCollection AddDomiumOpenTelemetry(
        this IServiceCollection services,
        Action<DomiumOpenTelemetryOptions>? configure = null)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        var options = new DomiumOpenTelemetryOptions();
        configure?.Invoke(options);

        services.Configure(configure ?? (_ => { }));

        return services.AddDomiumOpenTelemetry(options);
    }

    private static IServiceCollection AddDomiumOpenTelemetry(
        this IServiceCollection services,
        DomiumOpenTelemetryOptions options)
    {
        ValidateOptions(options);

        if (!options.Enabled)
        {
            return services;
        }

        var openTelemetryBuilder = services
            .AddOpenTelemetry()
            .ConfigureResource(resource => ConfigureResource(resource, options));

        if (options.EnableTracing)
        {
            openTelemetryBuilder.WithTracing(tracing =>
            {
                tracing.AddSource(DomiumTelemetry.ActivitySourceName);

                if (options.Otlp.Enabled)
                {
                    tracing.AddOtlpExporter(exporter => ConfigureOtlpExporter(exporter, options.Otlp));
                }
            });
        }

        if (options.EnableMetrics)
        {
            openTelemetryBuilder.WithMetrics(metrics =>
            {
                metrics.AddMeter(DomiumTelemetry.MeterName);

                if (options.Otlp.Enabled)
                {
                    metrics.AddOtlpExporter(exporter => ConfigureOtlpExporter(exporter, options.Otlp));
                }
            });
        }

        return services;
    }

    private static void ConfigureResource(ResourceBuilder resource, DomiumOpenTelemetryOptions options)
    {
        resource.AddService(
            serviceName: options.ServiceName,
            serviceVersion: options.ServiceVersion);

        var attributes = new List<KeyValuePair<string, object>>();

        if (!string.IsNullOrWhiteSpace(options.Environment))
        {
            attributes.Add(new KeyValuePair<string, object>(
                "deployment.environment.name",
                options.Environment));
        }

        foreach (var attribute in options.ResourceAttributes)
        {
            if (!string.IsNullOrWhiteSpace(attribute.Key) &&
                !string.IsNullOrWhiteSpace(attribute.Value))
            {
                attributes.Add(new KeyValuePair<string, object>(attribute.Key, attribute.Value));
            }
        }

        if (attributes.Count > 0)
        {
            resource.AddAttributes(attributes);
        }
    }

    private static void ConfigureOtlpExporter(
        OtlpExporterOptions exporter,
        DomiumOtlpExporterOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Endpoint))
        {
            exporter.Endpoint = new Uri(options.Endpoint);
        }

        if (!string.IsNullOrWhiteSpace(options.Headers))
        {
            exporter.Headers = options.Headers;
        }

        if (!Enum.TryParse<OtlpExportProtocol>(options.Protocol, ignoreCase: true, out var protocol))
        {
            throw new ArgumentException(
                $"Invalid OpenTelemetry OTLP protocol '{options.Protocol}'.",
                nameof(options));
        }

        exporter.Protocol = protocol;
    }

    private static void ValidateOptions(DomiumOpenTelemetryOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.ServiceName))
        {
            throw new ArgumentException("OpenTelemetry service name cannot be empty.", nameof(options));
        }
    }
}
