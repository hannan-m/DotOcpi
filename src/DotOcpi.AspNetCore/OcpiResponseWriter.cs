using System.Buffers;
using System.Text.Json;
using DotOcpi.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DotOcpi.AspNetCore;

/// <summary>
/// Writes OCPI responses in the standard envelope format with the
/// correct source-generated serializer context for the negotiated version.
/// </summary>
public static class OcpiResponseWriter
{
    private static DateTimeOffset GetTimestamp(HttpContext httpContext) =>
        httpContext.RequestServices?.GetService<TimeProvider>()?.GetUtcNow() ?? DateTimeOffset.UtcNow;

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
            Timestamp = GetTimestamp(httpContext),
        };

        var options = OcpiJsonOptions.GetOptions(version);
        return JsonSerializer.SerializeAsync(
            httpContext.Response.Body, envelope, options.GetTypeInfo(typeof(OcpiResponse<T>)), cancellationToken);
    }

    /// <summary>
    /// Writes a successful OCPI response with no data payload.
    /// Uses <see cref="Utf8JsonWriter"/> directly — fully NativeAOT-safe.
    /// </summary>
    public static async Task WriteSuccessNoDataAsync(
        HttpContext httpContext,
        int statusCode = 1000,
        string? statusMessage = null,
        CancellationToken cancellationToken = default
    )
    {
        httpContext.Response.ContentType = "application/json";

        var buffer = new ArrayBufferWriter<byte>(256);
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteNumber("status_code"u8, statusCode);
            if (statusMessage is not null)
                writer.WriteString("status_message"u8, statusMessage);
            writer.WriteString("timestamp"u8, GetTimestamp(httpContext));
            writer.WriteEndObject();
        }

        await httpContext.Response.Body.WriteAsync(buffer.WrittenMemory, cancellationToken).ConfigureAwait(false);
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
            writer.WriteString("timestamp"u8, GetTimestamp(httpContext));
            if (data is not null)
            {
                writer.WritePropertyName("data"u8);
                JsonSerializer.Serialize(writer, data, options.GetTypeInfo(data.GetType()));
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
            writer.WriteString("timestamp"u8, GetTimestamp(httpContext));
            writer.WriteStartArray("data"u8);
            foreach (var item in items)
            {
                JsonSerializer.Serialize(writer, item, options.GetTypeInfo(item.GetType()));
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        await httpContext.Response.Body.WriteAsync(buffer.WrittenMemory, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes an OCPI error response (no data payload).
    /// Uses <see cref="Utf8JsonWriter"/> directly — no reflection, no source-gen
    /// context needed, fully NativeAOT-safe.
    /// </summary>
    public static async Task WriteErrorAsync(
        HttpContext httpContext,
        int httpStatusCode,
        int ocpiStatusCode,
        string statusMessage,
        CancellationToken cancellationToken = default
    )
    {
        httpContext.Response.StatusCode = httpStatusCode;
        httpContext.Response.ContentType = "application/json";

        var buffer = new ArrayBufferWriter<byte>(256);
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteNumber("status_code"u8, ocpiStatusCode);
            writer.WriteString("status_message"u8, statusMessage);
            writer.WriteString("timestamp"u8, GetTimestamp(httpContext));
            writer.WriteEndObject();
        }

        await httpContext.Response.Body.WriteAsync(buffer.WrittenMemory, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates an <see cref="IResult"/> that writes an OCPI error response using
    /// <see cref="Utf8JsonWriter"/> — no anonymous object allocation.
    /// For use in endpoint filters that must return <see cref="IResult"/>.
    /// </summary>
    internal static IResult ErrorResult(int httpStatusCode, int ocpiStatusCode, string statusMessage) =>
        new OcpiErrorResult(httpStatusCode, ocpiStatusCode, statusMessage);

    /// <summary>
    /// Creates an <see cref="IResult"/> that writes an OCPI validation error response
    /// with a data array of error details — no anonymous object allocation.
    /// </summary>
    internal static IResult ValidationErrorResult(IReadOnlyList<Validation.OcpiValidationError> errors) =>
        new OcpiValidationErrorResult(errors);

    /// <summary>
    /// Writes an <see cref="OcpiResult"/> as an OCPI response (no data payload).
    /// </summary>
    internal static async Task WriteResultAsync(
        HttpContext httpContext,
        OcpiResult result,
        CancellationToken cancellationToken = default
    )
    {
        if (result.IsSuccess)
        {
            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            httpContext.Response.ContentType = "application/json";

            var buffer = new ArrayBufferWriter<byte>(256);
            using (var writer = new Utf8JsonWriter(buffer))
            {
                writer.WriteStartObject();
                writer.WriteNumber("status_code"u8, result.StatusCode.Value);
                if (result.StatusMessage is not null)
                    writer.WriteString("status_message"u8, result.StatusMessage);
                writer.WriteString("timestamp"u8, GetTimestamp(httpContext));
                writer.WriteEndObject();
            }

            await httpContext.Response.Body.WriteAsync(buffer.WrittenMemory, cancellationToken).ConfigureAwait(false);
            return;
        }

        var httpStatus = result.StatusCode.IsServerError
            ? StatusCodes.Status500InternalServerError
            : StatusCodes.Status400BadRequest;
        await WriteErrorAsync(
                httpContext,
                httpStatus,
                result.StatusCode.Value,
                result.StatusMessage ?? "Operation failed.",
                cancellationToken
            )
            .ConfigureAwait(false);
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

    private sealed class OcpiErrorResult(int httpStatusCode, int ocpiStatusCode, string statusMessage) : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext) =>
            WriteErrorAsync(httpContext, httpStatusCode, ocpiStatusCode, statusMessage, httpContext.RequestAborted);
    }

    private sealed class OcpiValidationErrorResult(IReadOnlyList<Validation.OcpiValidationError> errors) : IResult
    {
        public async Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            httpContext.Response.ContentType = "application/json";

            var buffer = new ArrayBufferWriter<byte>(512);
            using (var writer = new Utf8JsonWriter(buffer))
            {
                writer.WriteStartObject();
                writer.WriteNumber("status_code"u8, 2001);
                writer.WriteString("status_message"u8, "Invalid or missing parameters.");
                writer.WriteStartArray("data"u8);
                foreach (var error in errors)
                {
                    writer.WriteStartObject();
                    writer.WriteString("code"u8, error.Code);
                    writer.WriteString("message"u8, error.Message);
                    if (error.PropertyPath is not null)
                        writer.WriteString("property_path"u8, error.PropertyPath);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteString("timestamp"u8, GetTimestamp(httpContext));
                writer.WriteEndObject();
            }

            await httpContext.Response.Body.WriteAsync(buffer.WrittenMemory, httpContext.RequestAborted).ConfigureAwait(false);
        }
    }
}
