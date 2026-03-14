using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotOcpi.Serialization;

/// <summary>
/// Converts GeoLocation to/from OCPI wire format (object with string latitude/longitude).
/// </summary>
public sealed class GeoLocationConverter : JsonConverter<GeoLocation>
{
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

            var propertyName = reader.GetString();
            reader.Read();

            if (string.Equals(propertyName, "latitude", StringComparison.OrdinalIgnoreCase))
            {
                latitude = reader.GetString();
            }
            else if (string.Equals(propertyName, "longitude", StringComparison.OrdinalIgnoreCase))
            {
                longitude = reader.GetString();
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
        writer.WriteString("latitude", value.Latitude);
        writer.WriteString("longitude", value.Longitude);
        writer.WriteEndObject();
    }
}
