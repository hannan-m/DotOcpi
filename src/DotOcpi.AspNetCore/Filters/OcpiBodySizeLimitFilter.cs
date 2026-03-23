using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace DotOcpi.AspNetCore.Filters;

/// <summary>
/// Endpoint filter that rejects request bodies exceeding a configured
/// size limit. Returns HTTP 413 with OCPI status 2000.
/// Enforces limits both via Content-Length header and Kestrel's per-request
/// body size feature to prevent bypass via chunked transfer encoding.
/// </summary>
public sealed class OcpiBodySizeLimitFilter : IEndpointFilter
{
    private readonly long _maxBodySize;

    /// <summary>
    /// Creates a new body size limit filter.
    /// </summary>
    /// <param name="maxBodySizeBytes">Maximum allowed body size in bytes.</param>
    public OcpiBodySizeLimitFilter(long maxBodySizeBytes = 10 * 1024 * 1024)
    {
        _maxBodySize = maxBodySizeBytes;
    }

    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var contentLength = context.HttpContext.Request.ContentLength;
        if (contentLength > _maxBodySize)
        {
            return OcpiResponseWriter.ErrorResult(
                StatusCodes.Status413PayloadTooLarge,
                2000,
                $"Request body exceeds maximum size of {_maxBodySize} bytes."
            );
        }

        // Enforce at the Kestrel level for chunked requests without Content-Length
        var bodySizeFeature = context.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (bodySizeFeature is { IsReadOnly: false })
        {
            bodySizeFeature.MaxRequestBodySize = _maxBodySize;
        }

        return await next(context).ConfigureAwait(false);
    }
}
