using System.Buffers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace DotOcpi.Simulator.Infrastructure;

/// <summary>
/// Writes OCPI-compliant JSON responses with proper envelope, headers, and status codes.
/// </summary>
internal static class OcpiResponseWriter
{
    /// <summary>
    /// Reflection-based JSON options for serializing anonymous objects in legacy mode.
    /// Typed models use <see cref="Serialization.OcpiJsonOptions"/> instead.
    /// </summary>
    internal static readonly JsonSerializerOptions LegacyJsonOptions = new() { MaxDepth = 32 };

    /// <summary>
    /// Sets standard OCPI response headers: security headers, request/correlation IDs.
    /// </summary>
    public static void SetOcpiHeaders(HttpContext ctx)
    {
        // Echo or generate X-Request-ID
        if (!ctx.Request.Headers.TryGetValue("X-Request-ID", out var requestId) || requestId.Count == 0)
            requestId = Guid.NewGuid().ToString("N");
        ctx.Response.Headers["X-Request-ID"] = requestId;

        // Echo or generate X-Correlation-ID
        if (!ctx.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId) || correlationId.Count == 0)
            correlationId = Guid.NewGuid().ToString("N");
        ctx.Response.Headers["X-Correlation-ID"] = correlationId;

        // Security headers per CLAUDE.md requirements
        ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
        ctx.Response.Headers["Cache-Control"] = "no-store";
        ctx.Response.Headers["X-Frame-Options"] = "DENY";
    }

    /// <summary>
    /// Writes an OCPI envelope response with data written by a callback.
    /// </summary>
    public static async Task WriteEnvelopeAsync<TState>(
        HttpContext ctx,
        int ocpiStatusCode,
        Action<Utf8JsonWriter, TState>? writeData,
        TState state
    )
    {
        SetOcpiHeaders(ctx);
        ctx.Response.ContentType = "application/json";

        var buffer = new ArrayBufferWriter<byte>(1024);
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteNumber("status_code"u8, ocpiStatusCode);
            writer.WriteString("status_message"u8, ocpiStatusCode == 1000 ? "Success" : "Forced error");
            writer.WriteString("timestamp"u8, OcpiDateTime.Format(DateTimeOffset.UtcNow));
            writeData?.Invoke(writer, state);
            writer.WriteEndObject();
        }

        await ctx.Response.Body.WriteAsync(buffer.WrittenMemory, ctx.RequestAborted).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes an OCPI success envelope with data.
    /// </summary>
    public static Task WriteSuccessAsync<TState>(
        HttpContext ctx,
        Action<Utf8JsonWriter, TState> writeData,
        TState state
    ) => WriteEnvelopeAsync(ctx, 1000, writeData, state);

    /// <summary>
    /// Writes an OCPI success envelope with no data.
    /// </summary>
    public static Task WriteSuccessAsync(HttpContext ctx) => WriteEnvelopeAsync<object?>(ctx, 1000, null, null);

    /// <summary>
    /// Writes an OCPI error response.
    /// </summary>
    public static async Task WriteErrorAsync(HttpContext ctx, int httpStatusCode, int ocpiStatusCode, string message)
    {
        SetOcpiHeaders(ctx);
        ctx.Response.StatusCode = httpStatusCode;
        ctx.Response.ContentType = "application/json";

        var buffer = new ArrayBufferWriter<byte>(256);
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteNumber("status_code"u8, ocpiStatusCode);
            writer.WriteString("status_message"u8, message);
            writer.WriteString("timestamp"u8, OcpiDateTime.Format(DateTimeOffset.UtcNow));
            writer.WriteEndObject();
        }

        await ctx.Response.Body.WriteAsync(buffer.WrittenMemory, ctx.RequestAborted).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes an OCPI success envelope with a serialized data object using version-specific options.
    /// </summary>
    public static async Task WriteDataAsync(HttpContext ctx, OcpiVersion version, object data)
    {
        SetOcpiHeaders(ctx);
        ctx.Response.ContentType = "application/json";

        var options = Serialization.OcpiJsonOptions.GetOptions(version);

        var buffer = new ArrayBufferWriter<byte>(1024);
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteNumber("status_code"u8, 1000);
            writer.WriteString("status_message"u8, "Success");
            writer.WriteString("timestamp"u8, OcpiDateTime.Format(DateTimeOffset.UtcNow));
            writer.WritePropertyName("data"u8);
            JsonSerializer.Serialize(writer, data, data.GetType(), options);
            writer.WriteEndObject();
        }

        await ctx.Response.Body.WriteAsync(buffer.WrittenMemory, ctx.RequestAborted).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes an OCPI success envelope with a serialized data object using reflection (legacy/anonymous objects).
    /// </summary>
    public static async Task WriteLegacyDataAsync(HttpContext ctx, object data)
    {
        SetOcpiHeaders(ctx);
        ctx.Response.ContentType = "application/json";

        var buffer = new ArrayBufferWriter<byte>(1024);
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteNumber("status_code"u8, 1000);
            writer.WriteString("status_message"u8, "Success");
            writer.WriteString("timestamp"u8, OcpiDateTime.Format(DateTimeOffset.UtcNow));
            writer.WritePropertyName("data"u8);
            JsonSerializer.Serialize(writer, data, data.GetType(), LegacyJsonOptions);
            writer.WriteEndObject();
        }

        await ctx.Response.Body.WriteAsync(buffer.WrittenMemory, ctx.RequestAborted).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes an OCPI success envelope with a list of serialized data objects using reflection (legacy mode).
    /// </summary>
    public static async Task WriteLegacyListAsync(
        HttpContext ctx,
        IReadOnlyList<object> items,
        int totalCount,
        int offset,
        int limit,
        int ocpiStatusCode = 1000
    )
    {
        SetOcpiHeaders(ctx);
        ctx.Response.ContentType = "application/json";

        ctx.Response.Headers["X-Total-Count"] = totalCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        ctx.Response.Headers["X-Limit"] = limit.ToString(System.Globalization.CultureInfo.InvariantCulture);

        var nextOffset = offset + limit;
        if (nextOffset < totalCount)
        {
            var requestUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}{ctx.Request.Path}";
            var existingParams = string.Join(
                "&",
                ctx.Request.Query.Where(kv => kv.Key is not "offset" and not "limit")
                    .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value.ToString())}")
            );
            var separator = string.IsNullOrEmpty(existingParams) ? "" : $"&{existingParams}";
            ctx.Response.Headers["Link"] = $"<{requestUrl}?offset={nextOffset}&limit={limit}{separator}>; rel=\"next\"";
        }

        var statusMessage = ocpiStatusCode == 1000 ? "Success" : "Forced error";

        var buffer = new ArrayBufferWriter<byte>(1024);
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteNumber("status_code"u8, ocpiStatusCode);
            writer.WriteString("status_message"u8, statusMessage);
            writer.WriteString("timestamp"u8, OcpiDateTime.Format(DateTimeOffset.UtcNow));
            writer.WriteStartArray("data"u8);
            foreach (var item in items)
            {
                JsonSerializer.Serialize(writer, item, item.GetType(), LegacyJsonOptions);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        await ctx.Response.Body.WriteAsync(buffer.WrittenMemory, ctx.RequestAborted).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes an OCPI success envelope with a list of serialized data objects.
    /// </summary>
    public static async Task WriteListAsync(
        HttpContext ctx,
        OcpiVersion version,
        IReadOnlyList<object> items,
        int totalCount,
        int offset,
        int limit
    )
    {
        SetOcpiHeaders(ctx);
        ctx.Response.ContentType = "application/json";

        ctx.Response.Headers["X-Total-Count"] = totalCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        ctx.Response.Headers["X-Limit"] = limit.ToString(System.Globalization.CultureInfo.InvariantCulture);

        // Link header for next page
        var nextOffset = offset + limit;
        if (nextOffset < totalCount)
        {
            var requestUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}{ctx.Request.Path}";
            var existingParams = string.Join(
                "&",
                ctx.Request.Query.Where(kv => kv.Key is not "offset" and not "limit")
                    .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value.ToString())}")
            );
            var separator = string.IsNullOrEmpty(existingParams) ? "" : $"&{existingParams}";
            ctx.Response.Headers["Link"] = $"<{requestUrl}?offset={nextOffset}&limit={limit}{separator}>; rel=\"next\"";
        }

        var options = Serialization.OcpiJsonOptions.GetOptions(version);

        var buffer = new ArrayBufferWriter<byte>(1024);
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteNumber("status_code"u8, 1000);
            writer.WriteString("status_message"u8, "Success");
            writer.WriteString("timestamp"u8, OcpiDateTime.Format(DateTimeOffset.UtcNow));
            writer.WriteStartArray("data"u8);
            foreach (var item in items)
            {
                JsonSerializer.Serialize(writer, item, item.GetType(), options);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        await ctx.Response.Body.WriteAsync(buffer.WrittenMemory, ctx.RequestAborted).ConfigureAwait(false);
    }
}
