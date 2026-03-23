using System.Text.Json;
using System.Text.Json.Serialization;
using DotOcpi.Models.V2_0;

namespace DotOcpi.Serialization;

/// <summary>
/// Source-generated JSON serializer context for OCPI 2.0 models.
/// Uses snake_case naming, UTC date-times, and OCPI enum strings.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false,
    UseStringEnumConverter = true
)]
[JsonSerializable(typeof(Location))]
[JsonSerializable(typeof(Evse))]
[JsonSerializable(typeof(Connector))]
[JsonSerializable(typeof(Session))]
[JsonSerializable(typeof(Cdr))]
[JsonSerializable(typeof(Tariff))]
[JsonSerializable(typeof(Token))]
[JsonSerializable(typeof(AuthorizationInfo))]
[JsonSerializable(typeof(Credentials))]
[JsonSerializable(typeof(VersionDetail))]
[JsonSerializable(typeof(StartSession))]
[JsonSerializable(typeof(StopSession))]
[JsonSerializable(typeof(ReserveNow))]
[JsonSerializable(typeof(UnlockConnector))]
[JsonSerializable(typeof(CommandResponse))]
[JsonSerializable(typeof(LocationReferences))]
[JsonSerializable(typeof(OcpiResponse<Location>))]
[JsonSerializable(typeof(OcpiResponse<Session>))]
[JsonSerializable(typeof(OcpiResponse<Cdr>))]
[JsonSerializable(typeof(OcpiResponse<Tariff>))]
[JsonSerializable(typeof(OcpiResponse<Token>))]
[JsonSerializable(typeof(OcpiResponse<AuthorizationInfo>))]
[JsonSerializable(typeof(OcpiResponse<Credentials>))]
[JsonSerializable(typeof(OcpiResponse<CommandResponse>))]
[JsonSerializable(typeof(IReadOnlyList<Location>))]
[JsonSerializable(typeof(IReadOnlyList<Session>))]
[JsonSerializable(typeof(IReadOnlyList<Cdr>))]
[JsonSerializable(typeof(IReadOnlyList<Tariff>))]
[JsonSerializable(typeof(IReadOnlyList<Token>))]
[JsonSerializable(typeof(JsonElement))]
public sealed partial class OcpiJsonContext_V2_0 : JsonSerializerContext;
