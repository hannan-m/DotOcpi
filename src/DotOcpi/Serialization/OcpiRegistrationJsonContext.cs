using System.Text.Json.Serialization;
using DotOcpi.Registration;

namespace DotOcpi.Serialization;

/// <summary>
/// Source-generated JSON serializer context for version-agnostic
/// registration types used before OCPI version negotiation.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false
)]
[JsonSerializable(typeof(OcpiResponse<IReadOnlyList<VersionInfo>>))]
[JsonSerializable(typeof(OcpiResponse<VersionDetailInfo>))]
internal sealed partial class OcpiRegistrationJsonContext : JsonSerializerContext;
