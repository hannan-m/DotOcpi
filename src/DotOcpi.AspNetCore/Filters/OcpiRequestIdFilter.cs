using Microsoft.AspNetCore.Http;

namespace DotOcpi.AspNetCore.Filters;

/// <summary>
/// Endpoint filter that ensures X-Request-ID and X-Correlation-ID headers
/// are present on every OCPI request/response. Generates new IDs if missing,
/// echoes existing ones back in the response.
/// </summary>
public sealed class OcpiRequestIdFilter : IEndpointFilter
{
    internal const string RequestIdHeader = "X-Request-ID";
    internal const string CorrelationIdHeader = "X-Correlation-ID";

    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;

        var requestId = httpContext.Request.Headers[RequestIdHeader].ToString();
        if (string.IsNullOrWhiteSpace(requestId))
        {
            requestId = Guid.NewGuid().ToString("N");
        }

        var correlationId = httpContext.Request.Headers[CorrelationIdHeader].ToString();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        httpContext.SetRequestId(requestId);
        httpContext.SetCorrelationId(correlationId);

        // Set response headers early so they're present even if the handler throws
        httpContext.Response.Headers[RequestIdHeader] = requestId;
        httpContext.Response.Headers[CorrelationIdHeader] = correlationId;

        return await next(context).ConfigureAwait(false);
    }
}
