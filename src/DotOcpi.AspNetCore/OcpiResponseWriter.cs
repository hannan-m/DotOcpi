using System.Buffers;
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
    /// Writes a successful OCPI response where the data type is known only at runtime.
    /// Uses <see cref="JsonSerializer.Serialize(Utf8JsonWriter, object?, Type, JsonSerializerOptions?)"/>
    /// with the actual runtime type to ensure correct serialization of version-specific models.
    /// </summary>
    public static async Task WriteSuccessObjectAsync(
        HttpContext httpContext,
        object? data,
        OcpiVersion version,
        int statusCode = 1000,
        string? statusMessage = null,
        CancellationToken cancellationToken = default
    )
    {
        httpContext.Response.ContentType = "application/json";
        var options = OcpiJsonOptions.GetOptions(version);

        var buffer = new ArrayBufferWriter<byte>(512);
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteNumber("status_code"u8, statusCode);
            if (statusMessage is not null)
                writer.WriteString("status_message"u8, statusMessage);
            writer.WritePropertyName("timestamp"u8);
            JsonSerializer.Serialize(writer, DateTimeOffset.UtcNow, options);
            if (data is not null)
            {
                writer.WritePropertyName("data"u8);
                JsonSerializer.Serialize(writer, data, data.GetType(), options);
            }
            writer.WriteEndObject();
        }

        await httpContext.Response.Body.WriteAsync(buffer.WrittenMemory, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes a successful OCPI response with a list of runtime-typed items.
    /// Each item is serialized using its actual runtime type.
    /// </summary>
    public static async Task WriteSuccessListAsync(
        HttpContext httpContext,
        IReadOnlyList<object> items,
        OcpiVersion version,
        CancellationToken cancellationToken = default
    )
    {
        httpContext.Response.ContentType = "application/json";
        var options = OcpiJsonOptions.GetOptions(version);

        var buffer = new ArrayBufferWriter<byte>(1024);
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteNumber("status_code"u8, OcpiStatusCode.Success.Value);
            writer.WritePropertyName("timestamp"u8);
            JsonSerializer.Serialize(writer, DateTimeOffset.UtcNow, options);
            writer.WriteStartArray("data"u8);
            foreach (var item in items)
            {
                JsonSerializer.Serialize(writer, item, item.GetType(), options);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        await httpContext.Response.Body.WriteAsync(buffer.WrittenMemory, cancellationToken).ConfigureAwait(false);
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

    /// <summary>
    /// Writes an <see cref="OcpiResult"/> as an OCPI response (no data payload).
    /// </summary>
    internal static Task WriteResultAsync(
        HttpContext httpContext,
        OcpiResult result,
        OcpiVersion version,
        CancellationToken cancellationToken = default
    )
    {
        if (result.IsSuccess)
        {
            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            return WriteSuccessAsync<object?>(
                httpContext,
                null,
                version,
                result.StatusCode.Value,
                result.StatusMessage,
                cancellationToken
            );
        }

        var httpStatus = result.StatusCode.IsServerError
            ? StatusCodes.Status500InternalServerError
            : StatusCodes.Status400BadRequest;
        return WriteErrorAsync(
            httpContext,
            httpStatus,
            result.StatusCode.Value,
            result.StatusMessage ?? "Operation failed.",
            cancellationToken
        );
    }

    /// <summary>
    /// Writes an <see cref="OcpiResult{T}"/> with object data as an OCPI response.
    /// </summary>
    internal static Task WriteResultObjectAsync(
        HttpContext httpContext,
        OcpiResult<object> result,
        OcpiVersion version,
        CancellationToken cancellationToken = default
    )
    {
        if (result.IsSuccess)
        {
            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            return WriteSuccessObjectAsync(
                httpContext,
                result.Data,
                version,
                result.StatusCode.Value,
                result.StatusMessage,
                cancellationToken
            );
        }

        var httpStatus = result.StatusCode.IsServerError
            ? StatusCodes.Status500InternalServerError
            : StatusCodes.Status400BadRequest;
        return WriteErrorAsync(
            httpContext,
            httpStatus,
            result.StatusCode.Value,
            result.StatusMessage ?? "Operation failed.",
            cancellationToken
        );
    }
}
