using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotOcpi.Serialization;

/// <summary>
/// Converts DateTimeOffset to/from OCPI date-time format (RFC 3339 / ISO 8601 UTC).
/// </summary>
public sealed class OcpiDateTimeConverter : JsonConverter<DateTimeOffset>
{
    private const string Format = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        if (str is null)
        {
            throw new JsonException("Expected a date-time string but got null.");
        }

        if (DateTimeOffset.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result))
        {
            return result.ToUniversalTime();
        }

        throw new JsonException($"Unable to parse '{str}' as an OCPI date-time.");
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToUniversalTime().ToString(Format, CultureInfo.InvariantCulture));
    }
}
