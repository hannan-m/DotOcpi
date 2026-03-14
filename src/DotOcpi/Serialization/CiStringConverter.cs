using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotOcpi.Serialization;

/// <summary>
/// Converts CiString to/from a plain JSON string, preserving original case.
/// </summary>
public sealed class CiStringConverter : JsonConverter<CiString>
{
    public override CiString Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        return new CiString(str ?? "");
    }

    public override void Write(Utf8JsonWriter writer, CiString value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value ?? "");
    }
}
