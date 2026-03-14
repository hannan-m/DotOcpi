using System.Text.Json;
using DotOcpi.Serialization;
using Microsoft.AspNetCore.Http;

namespace DotOcpi.AspNetCore;

/// <summary>
/// Writes OCPI responses in the standard envelope format with the
/// correct source-generated serializer context for the negotiated version.
/// </summary>
public static class OcpiResponseWriter
{
    /// <summary>
    /// Writes a successful OCPI response with data payload.
    /// </summary>
    public static Task WriteSuccessAsync<T>(
        HttpContext httpContext,
        T data,
        OcpiVersion version,
        int statusCode = 1000,
        string? statusMessage = null,
        CancellationToken cancellationToken = default
    )
    {
        httpContext.Response.ContentType = "application/json";

        var envelope = new OcpiResponse<T>
        {
            Data = data,
            StatusCode = statusCode,
            StatusMessage = statusMessage,
            Timestamp = DateTimeOffset.UtcNow,
        };

        var options = OcpiJsonOptions.GetOptions(version);
        return JsonSerializer.SerializeAsync(httpContext.Response.Body, envelope, options, cancellationToken);
    }

    /// <summary>
    /// Writes an OCPI error response (no data payload).
    /// </summary>
    public static Task WriteErrorAsync(
        HttpContext httpContext,
        int httpStatusCode,
        int ocpiStatusCode,
        string statusMessage,
        CancellationToken cancellationToken = default
    )
    {
        httpContext.Response.StatusCode = httpStatusCode;
        httpContext.Response.ContentType = "application/json";

        var body = JsonSerializer.Serialize(
            new
            {
                status_code = ocpiStatusCode,
                status_message = statusMessage,
                timestamp = DateTimeOffset.UtcNow,
            }
        );

        return httpContext.Response.WriteAsync(body, cancellationToken);
    }
}
