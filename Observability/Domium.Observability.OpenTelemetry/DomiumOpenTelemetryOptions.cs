namespace Domium.Observability.OpenTelemetry;

/// <summary>
/// Options for Domium OpenTelemetry integration.
/// </summary>
public sealed class DomiumOpenTelemetryOptions
{
    public const string SectionName = "Domium:OpenTelemetry";

    public bool Enabled { get; set; } = true;

    public bool EnableTracing { get; set; } = true;

    public bool EnableMetrics { get; set; } = true;

    public string ServiceName { get; set; } = "Domium";

    public string? ServiceVersion { get; set; }

    public string? Environment { get; set; }

    public Dictionary<string, string> ResourceAttributes { get; set; } = new();

    public DomiumOtlpExporterOptions Otlp { get; set; } = new();
}
