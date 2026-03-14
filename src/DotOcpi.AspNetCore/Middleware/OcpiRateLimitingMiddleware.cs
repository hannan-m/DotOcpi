using System.Collections.Concurrent;
using System.Text.Json;
using DotOcpi.Registry;
using Microsoft.AspNetCore.Http;

namespace DotOcpi.AspNetCore.Middleware;

/// <summary>
/// Middleware that rate-limits OCPI requests per CPO connection.
/// Returns HTTP 429 with Retry-After header when the limit is exceeded.
/// </summary>
public sealed class OcpiRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly OcpiRateLimitOptions _options;
    private readonly ConcurrentDictionary<string, RateLimitEntry> _entries = new();

    /// <summary>
    /// Creates a new rate limiting middleware.
    /// </summary>
    public OcpiRateLimitingMiddleware(RequestDelegate next, OcpiRateLimitOptions options)
    {
        _next = next;
        _options = options;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    public async Task InvokeAsync(HttpContext httpContext)
    {
        var connection = httpContext.Items.TryGetValue(typeof(CpoConnection), out var value)
            ? value as CpoConnection
            : null;

        if (connection is null)
        {
            await _next(httpContext).ConfigureAwait(false);
            return;
        }

        var key = connection.ConnectionKey;
        var now = DateTimeOffset.UtcNow;

        var entry = _entries.AddOrUpdate(
            key,
            _ => new RateLimitEntry(1, now),
            (_, existing) =>
            {
                var elapsed = now - existing.WindowStart;
                if (elapsed >= _options.Window)
                {
                    return new RateLimitEntry(1, now);
                }
                return existing with { Count = existing.Count + 1 };
            }
        );

        if (entry.Count > _options.MaxRequestsPerWindow)
        {
            var remaining = _options.Window - (now - entry.WindowStart);
            var retryAfter = Math.Max(1, (int)Math.Ceiling(remaining.TotalSeconds));

            httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            httpContext.Response.Headers.RetryAfter = retryAfter.ToString(
                System.Globalization.CultureInfo.InvariantCulture
            );
            httpContext.Response.ContentType = "application/json";

            var body = JsonSerializer.Serialize(
                new
                {
                    status_code = 2000,
                    status_message = "Rate limit exceeded. Retry after the specified interval.",
                    timestamp = DateTimeOffset.UtcNow,
                }
            );

            await httpContext.Response.WriteAsync(body).ConfigureAwait(false);
            return;
        }

        await _next(httpContext).ConfigureAwait(false);
    }

    private sealed record RateLimitEntry(int Count, DateTimeOffset WindowStart);
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
