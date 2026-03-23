using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace DotOcpi.Observability;

/// <summary>
/// OCPI metrics instrumentation using System.Diagnostics.Metrics.
/// Consumers opt-in via <c>AddMeter("DotOcpi")</c> in their
/// OpenTelemetry metrics configuration.
/// </summary>
public sealed class OcpiMetrics
{
    /// <summary>
    /// The meter name consumers use to subscribe to DotOcpi metrics.
    /// </summary>
    public const string MeterName = "DotOcpi";

    private readonly Counter<long> _requestsTotal;
    private readonly Histogram<double> _requestDuration;
    private readonly UpDownCounter<long> _activeConnections;
    private readonly Counter<long> _authFailures;

    /// <summary>
    /// Creates a new OcpiMetrics instance with instruments registered
    /// on a meter created from the provided factory.
    /// </summary>
    public OcpiMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _requestsTotal = meter.CreateCounter<long>(
            "dotocpi.requests.total",
            description: "Total OCPI requests processed"
        );

        _requestDuration = meter.CreateHistogram<double>(
            "dotocpi.request.duration",
            unit: "s",
            description: "OCPI request processing duration"
        );

        _activeConnections = meter.CreateUpDownCounter<long>(
            "dotocpi.connections.active",
            description: "Number of active CPO connections"
        );

        _authFailures = meter.CreateCounter<long>("dotocpi.auth.failures", description: "OCPI authentication failures");
    }

    /// <summary>
    /// Records a completed OCPI request.
    /// </summary>
    public void RecordRequest(string direction, string module, string version, int statusCode)
    {
        var tags = new TagList
        {
            { "direction", direction },
            { "module", module },
            { "version", version },
            { "status", statusCode },
        };
        _requestsTotal.Add(1, tags);
    }

    /// <summary>
    /// Records the duration of an OCPI request in seconds.
    /// </summary>
    public void RecordRequestDuration(double durationSeconds, string direction, string module, string version)
    {
        var tags = new TagList
        {
            { "direction", direction },
            { "module", module },
            { "version", version },
        };
        _requestDuration.Record(durationSeconds, tags);
    }

    /// <summary>
    /// Increments the active CPO connection count.
    /// </summary>
    public void ConnectionActivated(string status)
    {
        _activeConnections.Add(1, new TagList { { "status", status } });
    }

    /// <summary>
    /// Decrements the active CPO connection count.
    /// </summary>
    public void ConnectionDeactivated(string status)
    {
        _activeConnections.Add(-1, new TagList { { "status", status } });
    }

    /// <summary>
    /// Records an authentication failure.
    /// </summary>
    public void RecordAuthFailure(string reason)
    {
        _authFailures.Add(1, new TagList { { "reason", reason } });
    }
}
