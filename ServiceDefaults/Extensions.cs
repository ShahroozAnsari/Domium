using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Cross-cutting host setup every API shares: OpenTelemetry, health endpoints, HTTP
    /// resilience, and the domain exception handler.
    ///
    /// This began as Aspire's service-defaults project. Aspire's own pieces — service discovery
    /// and orchestrator-injected connection strings — are gone: under Docker Compose a service is
    /// reached by its compose name and configured by environment variables, so there is nothing
    /// left for discovery to resolve. Everything else here is plain .NET and still earns its keep.
    /// </summary>
    public static class Extensions
    {
        private const string HealthEndpointPath = "/health";
        private const string AlivenessEndpointPath = "/alive";

        public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
        {
            builder.ConfigureOpenTelemetry();

            builder.AddDefaultHealthChecks();

            // Domain rule violations become 409/404 ProblemDetails with the (localized)
            // domain message instead of opaque 500s.
            builder.Services.AddExceptionHandler<DomainExceptionHandler>();

            // Retries and circuit breaking on every outgoing call — the service-to-service
            // provisioning hops between Identity, Fleet and Tracking depend on it.
            builder.Services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());

            return builder;
        }

        public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
        {
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });

            builder.Services.AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    metrics.AddAspNetCoreInstrumentation()
                           .AddHttpClientInstrumentation()
                           .AddRuntimeInstrumentation()
                           .AddSqlClientInstrumentation()
                           .AddMeter("Microsoft.AspNetCore.Hosting")
                           .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
                           .AddMeter("MassTransit");
                })
                .WithTracing(tracing =>
                {
                    tracing.AddSource(builder.Environment.ApplicationName)
                           .AddSource("MassTransit")
                           // Health probes run constantly under compose; tracing them is noise.
                           .AddAspNetCoreInstrumentation(options =>
                               options.Filter = context =>
                                   !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                                   && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath))
                           .AddHttpClientInstrumentation()
                           .AddSqlClientInstrumentation(options => options.RecordException = true)
                           .AddEntityFrameworkCoreInstrumentation(options =>
                           {
                               options.EnrichWithIDbCommand = (activity, command) =>
                               {
                                   activity.SetTag("db.statement", command.CommandText);
                               };
                           })
                           .AddRedisInstrumentation();
                });

            builder.AddOpenTelemetryExporters();

            return builder;
        }

        /// <summary>
        /// Exports only when an OTLP endpoint is configured. Compose leaves
        /// OTEL_EXPORTER_OTLP_ENDPOINT unset by default, so telemetry stays in-process until a
        /// collector is pointed at it — there is no Aspire dashboard to fall back on any more.
        /// </summary>
        private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
        {
            var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

            if (useOtlpExporter)
            {
                builder.Services.AddOpenTelemetry().UseOtlpExporter();
            }

            return builder;
        }

        public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
        {
            builder.Services.AddHealthChecks()
                // Liveness: the process is responsive, whether or not its dependencies are.
                .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

            return builder;
        }

        /// <summary>
        /// Health endpoints, mapped in every environment rather than only in development.
        ///
        /// Compose depends on them: a healthcheck is what makes
        /// <c>depends_on: condition: service_healthy</c> mean anything, and without it the APIs
        /// race their databases on startup. They expose up/down and nothing else.
        /// </summary>
        public static WebApplication MapDefaultEndpoints(this WebApplication app)
        {
            app.MapHealthChecks(HealthEndpointPath);

            app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });

            return app;
        }
    }
}
