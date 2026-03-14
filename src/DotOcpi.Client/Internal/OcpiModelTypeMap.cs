namespace DotOcpi.Client.Internal;

/// <summary>
/// Maps OCPI versions to their version-specific model types for runtime deserialization.
/// </summary>
internal static class OcpiModelTypeMap
{
    internal static Type GetLocationType(OcpiVersion version) =>
        version switch
        {
            OcpiVersion.V2_0 => typeof(Models.V2_0.Location),
            OcpiVersion.V2_1_1 => typeof(Models.V2_1_1.Location),
            OcpiVersion.V2_2 => typeof(Models.V2_2.Location),
            OcpiVersion.V2_2_1 => typeof(Models.V2_2_1.Location),
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, "Unsupported OCPI version."),
        };

    internal static Type GetSessionType(OcpiVersion version) =>
        version switch
        {
            OcpiVersion.V2_0 => typeof(Models.V2_0.Session),
            OcpiVersion.V2_1_1 => typeof(Models.V2_1_1.Session),
            OcpiVersion.V2_2 => typeof(Models.V2_2.Session),
            OcpiVersion.V2_2_1 => typeof(Models.V2_2_1.Session),
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, "Unsupported OCPI version."),
        };

    internal static Type GetCdrType(OcpiVersion version) =>
        version switch
        {
            OcpiVersion.V2_0 => typeof(Models.V2_0.Cdr),
            OcpiVersion.V2_1_1 => typeof(Models.V2_1_1.Cdr),
            OcpiVersion.V2_2 => typeof(Models.V2_2.Cdr),
            OcpiVersion.V2_2_1 => typeof(Models.V2_2_1.Cdr),
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, "Unsupported OCPI version."),
        };

    internal static Type GetTariffType(OcpiVersion version) =>
        version switch
        {
            OcpiVersion.V2_0 => typeof(Models.V2_0.Tariff),
            OcpiVersion.V2_1_1 => typeof(Models.V2_1_1.Tariff),
            OcpiVersion.V2_2 => typeof(Models.V2_2.Tariff),
            OcpiVersion.V2_2_1 => typeof(Models.V2_2_1.Tariff),
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, "Unsupported OCPI version."),
        };

    internal static Type GetTokenType(OcpiVersion version) =>
        version switch
        {
            OcpiVersion.V2_0 => typeof(Models.V2_0.Token),
            OcpiVersion.V2_1_1 => typeof(Models.V2_1_1.Token),
            OcpiVersion.V2_2 => typeof(Models.V2_2.Token),
            OcpiVersion.V2_2_1 => typeof(Models.V2_2_1.Token),
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, "Unsupported OCPI version."),
        };

    internal static Type GetCommandResponseType(OcpiVersion version) =>
        version switch
        {
            OcpiVersion.V2_0 => typeof(Models.V2_0.CommandResponse),
            OcpiVersion.V2_1_1 => typeof(Models.V2_1_1.CommandResponse),
            OcpiVersion.V2_2 => typeof(Models.V2_2.CommandResponse),
            OcpiVersion.V2_2_1 => typeof(Models.V2_2_1.CommandResponse),
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, "Unsupported OCPI version."),
        };

    internal static Type GetChargingProfileResponseType(OcpiVersion version) =>
        version switch
        {
            OcpiVersion.V2_2 => typeof(Models.V2_2.ChargingProfileResponse),
            OcpiVersion.V2_2_1 => typeof(Models.V2_2_1.ChargingProfileResponse),
            _ => throw new ArgumentOutOfRangeException(
                nameof(version),
                version,
                "Charging profiles are only available in OCPI 2.2 and later."
            ),
        };
}
