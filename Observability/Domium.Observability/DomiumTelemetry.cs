using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Domium.Observability;

/// <summary>
/// Central Domium diagnostics source names and instruments.
/// </summary>
public static class DomiumTelemetry
{
    public const string ActivitySourceName = "Domium";

    public const string MeterName = "Domium";

    public static readonly ActivitySource ActivitySource = new ActivitySource(ActivitySourceName);

    public static readonly Meter Meter = new Meter(MeterName);

    public static readonly Counter<long> CommandsExecuted = Meter.CreateCounter<long>(
        "domium.commands.executed",
        description: "Number of commands executed through Domium.");

    public static readonly Counter<long> QueriesExecuted = Meter.CreateCounter<long>(
        "domium.queries.executed",
        description: "Number of queries executed through Domium.");

    public static readonly Counter<long> InternalEventsPublished = Meter.CreateCounter<long>(
        "domium.internal_events.published",
        description: "Number of internal events published through Domium.");

    public static readonly Counter<long> ExternalEventsPublished = Meter.CreateCounter<long>(
        "domium.external_events.published",
        description: "Number of external events published through Domium.");

    public static readonly Histogram<double> OperationDuration = Meter.CreateHistogram<double>(
        "domium.operation.duration",
        unit: "ms",
        description: "Duration of Domium operations in milliseconds.");
}
