using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotOcpi.Serialization;

/// <summary>
/// Provides pre-configured JsonSerializerOptions per OCPI version,
/// wired to the corresponding source-generated <see cref="JsonSerializerContext"/>
/// for AOT-safe, zero-reflection serialization of all declared model types.
/// </summary>
public static class OcpiJsonOptions
{
    private static JsonSerializerOptions? _v2_2_1;
    private static JsonSerializerOptions? _v2_2;
    private static JsonSerializerOptions? _v2_1_1;
    private static JsonSerializerOptions? _v2_0;

    /// <summary>
    /// Gets the JsonSerializerOptions for the given OCPI version.
    /// </summary>
    public static JsonSerializerOptions GetOptions(OcpiVersion version) =>
        version switch
        {
            OcpiVersion.V2_2_1 => V2_2_1,
            OcpiVersion.V2_2 => V2_2,
            OcpiVersion.V2_1_1 => V2_1_1,
            OcpiVersion.V2_0 => V2_0,
            _ => throw new ArgumentOutOfRangeException(nameof(version)),
        };

    /// <summary>Options configured for OCPI 2.2.1.</summary>
    public static JsonSerializerOptions V2_2_1 => _v2_2_1 ??= CreateOptions(OcpiJsonContext_V2_2_1.Default);

    /// <summary>Options configured for OCPI 2.2.</summary>
    public static JsonSerializerOptions V2_2 => _v2_2 ??= CreateOptions(OcpiJsonContext_V2_2.Default);

    /// <summary>Options configured for OCPI 2.1.1.</summary>
    public static JsonSerializerOptions V2_1_1 => _v2_1_1 ??= CreateOptions(OcpiJsonContext_V2_1_1.Default);

    /// <summary>Options configured for OCPI 2.0.</summary>
    public static JsonSerializerOptions V2_0 => _v2_0 ??= CreateOptions(OcpiJsonContext_V2_0.Default);

    private static JsonSerializerOptions CreateOptions(JsonSerializerContext context)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            MaxDepth = 32,
            WriteIndented = false,
        };

        options.TypeInfoResolverChain.Add(context);

        options.Converters.Add(new OcpiDateTimeConverter());
        options.Converters.Add(new CiStringConverter());
        options.Converters.Add(new GeoLocationConverter());

        options.MakeReadOnly();
        return options;
    }
}
