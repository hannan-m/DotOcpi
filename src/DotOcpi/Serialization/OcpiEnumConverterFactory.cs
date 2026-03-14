using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotOcpi.Serialization;

/// <summary>
/// Factory that produces JsonStringEnumConverter instances for all OCPI enums.
/// Enums are serialized as their member names (UPPERCASE as defined in the enum).
/// </summary>
public sealed class OcpiEnumConverterFactory : JsonConverterFactory
{
    private readonly JsonStringEnumConverter _inner = new();

    public override bool CanConvert(Type typeToConvert) => _inner.CanConvert(typeToConvert);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        _inner.CreateConverter(typeToConvert, options);
}
