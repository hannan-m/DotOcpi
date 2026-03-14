using Microsoft.AspNetCore.Http;

namespace DotOcpi.AspNetCore;

/// <summary>
/// Extension methods for storing and retrieving OCPI context from <see cref="HttpContext.Items"/>.
/// </summary>
public static class OcpiHttpContextExtensions
{
    private static readonly object RequestContextKey = new();
    private static readonly object RequestIdKey = new();
    private static readonly object CorrelationIdKey = new();

    /// <summary>
    /// Gets the <see cref="OcpiRequestContext"/> from the current request, or null if not set.
    /// </summary>
    public static OcpiRequestContext? GetOcpiContext(this HttpContext httpContext) =>
        httpContext.Items.TryGetValue(RequestContextKey, out var value) ? value as OcpiRequestContext : null;

    /// <summary>
    /// Sets the <see cref="OcpiRequestContext"/> for the current request.
    /// </summary>
    public static void SetOcpiContext(this HttpContext httpContext, OcpiRequestContext context) =>
        httpContext.Items[RequestContextKey] = context;

    /// <summary>
    /// Gets the X-Request-ID for the current request, or null if not set.
    /// </summary>
    public static string? GetRequestId(this HttpContext httpContext) =>
        httpContext.Items.TryGetValue(RequestIdKey, out var value) ? value as string : null;

    /// <summary>
    /// Sets the X-Request-ID for the current request.
    /// </summary>
    public static void SetRequestId(this HttpContext httpContext, string requestId) =>
        httpContext.Items[RequestIdKey] = requestId;

    /// <summary>
    /// Gets the X-Correlation-ID for the current request, or null if not set.
    /// </summary>
    public static string? GetCorrelationId(this HttpContext httpContext) =>
        httpContext.Items.TryGetValue(CorrelationIdKey, out var value) ? value as string : null;

    /// <summary>
    /// Sets the X-Correlation-ID for the current request.
    /// </summary>
    public static void SetCorrelationId(this HttpContext httpContext, string correlationId) =>
        httpContext.Items[CorrelationIdKey] = correlationId;
}
