using System.Diagnostics;

namespace DotOcpi.Observability;

/// <summary>
/// Centralized ActivitySource for DotOcpi distributed tracing.
/// Consumers opt-in via <c>AddSource("DotOcpi")</c> in their
/// OpenTelemetry tracing configuration.
/// </summary>
internal static class OcpiActivitySource
{
    /// <summary>
    /// The source name consumers use to subscribe to DotOcpi traces.
    /// </summary>
    public const string SourceName = "DotOcpi";

    /// <summary>
    /// Shared ActivitySource instance for all DotOcpi operations.
    /// </summary>
    public static readonly ActivitySource Source = new(SourceName);

    /// <summary>
    /// Starts an activity for an outbound OCPI client request.
    /// Returns null when no listener is subscribed (zero overhead).
    /// </summary>
    public static Activity? StartClientRequest(string method, string cpoId, string module)
    {
        var activity = Source.StartActivity($"OCPI {method} {module}", ActivityKind.Client);
        if (activity is not null)
        {
            activity.SetTag("ocpi.direction", "outbound");
            activity.SetTag("ocpi.cpo.id", cpoId);
            activity.SetTag("ocpi.module", module);
            activity.SetTag("http.method", method);
        }

        return activity;
    }

    /// <summary>
    /// Starts an activity for an inbound OCPI server request.
    /// Returns null when no listener is subscribed (zero overhead).
    /// </summary>
    public static Activity? StartServerRequest(string method, string path, string? cpoId)
    {
        var activity = Source.StartActivity($"OCPI {method} {path}", ActivityKind.Server);
        if (activity is not null)
        {
            activity.SetTag("ocpi.direction", "inbound");
            activity.SetTag("http.method", method);
            activity.SetTag("http.route", path);
            if (cpoId is not null)
            {
                activity.SetTag("ocpi.cpo.id", cpoId);
            }
        }

        return activity;
    }

    /// <summary>
    /// Starts an activity for a registration handshake.
    /// Returns null when no listener is subscribed (zero overhead).
    /// </summary>
    public static Activity? StartRegistration(string cpoId)
    {
        var activity = Source.StartActivity("OCPI Registration", ActivityKind.Client);
        activity?.SetTag("ocpi.cpo.id", cpoId);
        return activity;
    }

    /// <summary>
    /// Sets OCPI-specific tags on an existing activity after the response is available.
    /// </summary>
    public static void SetResponseTags(Activity? activity, OcpiVersion version, int ocpiStatusCode, int httpStatusCode)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("ocpi.version", version.ToString());
        activity.SetTag("ocpi.status_code", ocpiStatusCode);
        activity.SetTag("http.status_code", httpStatusCode);

        if (ocpiStatusCode >= 2000)
        {
            activity.SetStatus(ActivityStatusCode.Error);
        }
    }
}
