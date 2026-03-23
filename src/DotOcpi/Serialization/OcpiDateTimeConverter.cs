using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotOcpi.Serialization;

/// <summary>
/// Converts DateTimeOffset to/from OCPI date-time format (RFC 3339 / ISO 8601 UTC).
/// Uses span-based APIs to avoid string allocations on both read and write paths.
/// </summary>
public sealed class OcpiDateTimeConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // TryGetDateTimeOffset parses RFC 3339 directly from the UTF-8 span — zero string allocation.
        // Dates without timezone offset are treated as UTC by the reader.
        if (reader.TryGetDateTimeOffset(out var result))
        {
            return result.ToUniversalTime();
        }

        throw new JsonException($"Unable to parse '{reader.GetString()}' as an OCPI date-time.");
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        var utc = value.ToUniversalTime();
        // IUtf8SpanFormattable — formats directly to stack buffer, no string allocation.
        // "yyyy-MM-ddTHH:mm:ssZ" = 20 bytes
        Span<byte> buffer = stackalloc byte[20];
        ((IUtf8SpanFormattable)utc).TryFormat(buffer, out _, "yyyy-MM-dd'T'HH:mm:ss'Z'", null);
        writer.WriteStringValue(buffer);
    }
}
