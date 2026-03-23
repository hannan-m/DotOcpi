using System.Diagnostics;
using DotOcpi.Observability;
using DotOcpi.Registry;
using Microsoft.AspNetCore.Http;

namespace DotOcpi.AspNetCore.Filters;

/// <summary>
/// Endpoint filter that records OCPI request metrics (count and duration).
/// Runs after <see cref="OcpiAuthFilter"/> to access the authenticated
/// <see cref="CpoConnection"/> for version and module tagging.
/// </summary>
public sealed class OcpiMetricsFilter : IEndpointFilter
{
    private readonly OcpiMetrics _metrics;

    public OcpiMetricsFilter(OcpiMetrics metrics)
    {
        _metrics = metrics;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var startTimestamp = Stopwatch.GetTimestamp();

        var result = await next(context).ConfigureAwait(false);

        var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

        var connection = httpContext.Items.TryGetValue(typeof(CpoConnection), out var value)
            ? value as CpoConnection
            : null;

        var version = connection?.Version.ToVersionString() ?? "unknown";
        var ocpiContext = httpContext.GetOcpiContext();
        var module = ocpiContext?.ModuleId ?? "unknown";
        var statusCode = httpContext.Response.StatusCode;

        _metrics.RecordRequest("inbound", module, version, statusCode);
        _metrics.RecordRequestDuration(elapsed.TotalSeconds, "inbound", module, version);

        return result;
    }
}
