namespace Domium.Observability.OpenTelemetry;

/// <summary>
/// Options for the OpenTelemetry Protocol exporter.
/// </summary>
public sealed class DomiumOtlpExporterOptions
{
    public bool Enabled { get; set; }

    public string? Endpoint { get; set; }

    public string? Headers { get; set; }

    public string Protocol { get; set; } = "Grpc";
}
