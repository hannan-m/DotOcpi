using Microsoft.AspNetCore.Http;

namespace DotOcpi.AspNetCore.Filters;

/// <summary>
/// Endpoint filter that rejects request bodies exceeding a configured
/// size limit. Returns HTTP 413 with OCPI status 2000.
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

        return await next(context).ConfigureAwait(false);
    }
}
