using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotOcpi.Serialization;

/// <summary>
/// Converts nullable CiString to/from a plain JSON string.
/// </summary>
public sealed class NullableCiStringConverter : JsonConverter<CiString?>
{
    public override CiString? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        var str = reader.GetString();
        if (str is null)
        {
            return default;
        }

        return new CiString(str);
    }

    public override void Write(Utf8JsonWriter writer, CiString? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.Value ?? "");
    }
}
