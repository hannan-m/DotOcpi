using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DotOcpi.AspNetCore.Middleware;

/// <summary>
/// Middleware that catches unhandled exceptions and returns a
/// standard OCPI error response. No stack traces leak to the client.
/// </summary>
public sealed partial class OcpiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<OcpiExceptionMiddleware> _logger;

    /// <summary>
    /// Creates a new exception middleware instance.
    /// </summary>
    public OcpiExceptionMiddleware(RequestDelegate next, ILogger<OcpiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected — log and suppress
            LogRequestCancelled(httpContext.Request.Path);
        }
        catch (Exception ex)
        {
            LogUnhandledException(ex, httpContext.Request.Path);

            if (!httpContext.Response.HasStarted)
            {
                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                httpContext.Response.ContentType = "application/json";

                var body = JsonSerializer.Serialize(
                    new
                    {
                        status_code = 3000,
                        status_message = "Internal server error.",
                        timestamp = DateTimeOffset.UtcNow,
                    }
                );

                await httpContext.Response.WriteAsync(body).ConfigureAwait(false);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "OCPI request cancelled by client: {Path}")]
    private partial void LogRequestCancelled(string path);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception processing OCPI request: {Path}")]
    private partial void LogUnhandledException(Exception exception, string path);
}
