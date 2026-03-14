using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotOcpi.Serialization;

/// <summary>
/// Converts nullable DateTimeOffset to/from OCPI date-time format.
/// </summary>
public sealed class OcpiNullableDateTimeConverter : JsonConverter<DateTimeOffset?>
{
    private const string Format = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        var str = reader.GetString();
        if (str is null)
        {
            return null;
        }

        if (DateTimeOffset.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result))
        {
            return result.ToUniversalTime();
        }

        throw new JsonException($"Unable to parse '{str}' as an OCPI date-time.");
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.ToUniversalTime().ToString(Format, CultureInfo.InvariantCulture));
    }
}
