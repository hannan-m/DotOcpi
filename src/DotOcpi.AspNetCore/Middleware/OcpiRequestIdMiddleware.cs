using Microsoft.AspNetCore.Http;

namespace DotOcpi.AspNetCore.Middleware;

/// <summary>
/// Middleware that ensures X-Request-ID and X-Correlation-ID headers are
/// present on every OCPI response — including error responses from the
/// exception handler, 404s, and auth rejections. Registered as the
/// outermost middleware so it wraps the entire pipeline.
/// Uses <see cref="HttpContext.TraceIdentifier"/> as the default ID,
/// avoiding a <see cref="Guid"/> allocation when no header is provided.
/// </summary>
public sealed class OcpiRequestIdMiddleware
{
    private readonly RequestDelegate _next;

    public OcpiRequestIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        var requestId = httpContext.Request.Headers["X-Request-ID"].ToString();
        if (string.IsNullOrWhiteSpace(requestId))
            requestId = httpContext.TraceIdentifier;

        var correlationId = httpContext.Request.Headers["X-Correlation-ID"].ToString();
        if (string.IsNullOrWhiteSpace(correlationId))
            correlationId = httpContext.TraceIdentifier;

        httpContext.SetRequestId(requestId);
        httpContext.SetCorrelationId(correlationId);

        // OnStarting ensures headers are set even if the handler throws
        // or the pipeline short-circuits.
        httpContext.Response.OnStarting(
            static state =>
            {
                var (ctx, reqId, corrId) = ((HttpContext, string, string))state;
                ctx.Response.Headers["X-Request-ID"] = reqId;
                ctx.Response.Headers["X-Correlation-ID"] = corrId;
                return Task.CompletedTask;
            },
            (httpContext, requestId, correlationId)
        );

        await _next(httpContext).ConfigureAwait(false);
    }
}
