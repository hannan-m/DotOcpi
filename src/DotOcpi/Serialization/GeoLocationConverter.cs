using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotOcpi.Serialization;

/// <summary>
/// Converts GeoLocation to/from OCPI wire format (object with string latitude/longitude).
/// Uses <see cref="Utf8JsonReader.ValueTextEquals(ReadOnlySpan{byte})"/> for zero-allocation
/// property name matching.
/// </summary>
public sealed class GeoLocationConverter : JsonConverter<GeoLocation>
{
    private static ReadOnlySpan<byte> LatitudeProperty => "latitude"u8;
    private static ReadOnlySpan<byte> LongitudeProperty => "longitude"u8;

    public override GeoLocation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected start of object for GeoLocation.");
        }

        string? latitude = null;
        string? longitude = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected property name.");
            }

            if (reader.ValueTextEquals(LatitudeProperty))
            {
                reader.Read();
                latitude = reader.GetString();
            }
            else if (reader.ValueTextEquals(LongitudeProperty))
            {
                reader.Read();
                longitude = reader.GetString();
            }
            else
            {
                reader.Read();
                reader.Skip();
            }
        }

        if (latitude is null || longitude is null)
        {
            throw new JsonException("GeoLocation requires both latitude and longitude.");
        }

        return new GeoLocation(latitude, longitude);
    }

    public override void Write(Utf8JsonWriter writer, GeoLocation value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(LatitudeProperty, value.Latitude);
        writer.WriteString(LongitudeProperty, value.Longitude);
        writer.WriteEndObject();
    }
}
