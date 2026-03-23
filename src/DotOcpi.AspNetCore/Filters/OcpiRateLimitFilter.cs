using System.Threading.RateLimiting;
using DotOcpi.Registry;
using Microsoft.AspNetCore.Http;

namespace DotOcpi.AspNetCore.Filters;

/// <summary>
/// Endpoint filter that rate-limits OCPI requests per CPO connection using the
/// runtime's <see cref="PartitionedRateLimiter{TResource}"/>.
/// Runs after <see cref="OcpiAuthFilter"/> so that
/// <see cref="CpoConnection"/> is available in <see cref="HttpContext.Items"/>.
/// Returns HTTP 429 with Retry-After header when the limit is exceeded.
/// </summary>
public sealed class OcpiRateLimitFilter : IEndpointFilter, IDisposable
{
    private readonly PartitionedRateLimiter<string> _limiter;

    public OcpiRateLimitFilter(OcpiRateLimitOptions options)
    {
        _limiter = PartitionedRateLimiter.Create<string, string>(key =>
            RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = options.MaxRequestsPerWindow,
                Window = options.Window,
                AutoReplenishment = true,
                QueueLimit = 0,
            }));
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var connection = httpContext.Items.TryGetValue(typeof(CpoConnection), out var value)
            ? value as CpoConnection
            : null;

        if (connection is null)
            return await next(context).ConfigureAwait(false);

        using var lease = _limiter.AttemptAcquire(connection.ConnectionKey);
        if (lease.IsAcquired)
            return await next(context).ConfigureAwait(false);

        if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            httpContext.Response.Headers.RetryAfter =
                Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds))
                    .ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return OcpiResponseWriter.ErrorResult(
            StatusCodes.Status429TooManyRequests,
            2000,
            "Rate limit exceeded. Retry after the specified interval."
        );
    }

    public void Dispose() => _limiter.Dispose();
}

/// <summary>
/// Configuration options for OCPI rate limiting.
/// </summary>
public sealed class OcpiRateLimitOptions
{
    /// <summary>Maximum requests allowed per CPO within the time window.</summary>
    public int MaxRequestsPerWindow { get; set; } = 100;

    /// <summary>The time window for rate limiting.</summary>
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}
