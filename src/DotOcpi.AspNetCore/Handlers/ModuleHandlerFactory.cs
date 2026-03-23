using DotOcpi.Serialization;
using V2_0 = DotOcpi.Models.V2_0;
using V2_1_1 = DotOcpi.Models.V2_1_1;
using V2_2 = DotOcpi.Models.V2_2;
using V2_2_1 = DotOcpi.Models.V2_2_1;

namespace DotOcpi.AspNetCore.Handlers;

/// <summary>
/// Provides pre-cached, version-specific module handlers for OCPI request body
/// deserialization. Handlers are stateless and shared — no per-request allocation.
/// </summary>
internal static class ModuleHandlerFactory
{
    // Location
    private static readonly IModuleHandler LocationV20 = new ModuleHandler<V2_0.Location>(OcpiJsonOptions.V2_0);
    private static readonly IModuleHandler LocationV211 = new ModuleHandler<V2_1_1.Location>(OcpiJsonOptions.V2_1_1);
    private static readonly IModuleHandler LocationV22 = new ModuleHandler<V2_2.Location>(OcpiJsonOptions.V2_2);
    private static readonly IModuleHandler LocationV221 = new ModuleHandler<V2_2_1.Location>(OcpiJsonOptions.V2_2_1);

    internal static IModuleHandler Location(OcpiVersion version) => version switch
    {
        OcpiVersion.V2_0 => LocationV20,
        OcpiVersion.V2_1_1 => LocationV211,
        OcpiVersion.V2_2 => LocationV22,
        OcpiVersion.V2_2_1 => LocationV221,
        _ => throw new ArgumentOutOfRangeException(nameof(version)),
    };

    // Evse
    private static readonly IModuleHandler EvseV20 = new ModuleHandler<V2_0.Evse>(OcpiJsonOptions.V2_0);
    private static readonly IModuleHandler EvseV211 = new ModuleHandler<V2_1_1.Evse>(OcpiJsonOptions.V2_1_1);
    private static readonly IModuleHandler EvseV22 = new ModuleHandler<V2_2.Evse>(OcpiJsonOptions.V2_2);
    private static readonly IModuleHandler EvseV221 = new ModuleHandler<V2_2_1.Evse>(OcpiJsonOptions.V2_2_1);

    internal static IModuleHandler Evse(OcpiVersion version) => version switch
    {
        OcpiVersion.V2_0 => EvseV20,
        OcpiVersion.V2_1_1 => EvseV211,
        OcpiVersion.V2_2 => EvseV22,
        OcpiVersion.V2_2_1 => EvseV221,
        _ => throw new ArgumentOutOfRangeException(nameof(version)),
    };

    // Connector
    private static readonly IModuleHandler ConnectorV20 = new ModuleHandler<V2_0.Connector>(OcpiJsonOptions.V2_0);
    private static readonly IModuleHandler ConnectorV211 = new ModuleHandler<V2_1_1.Connector>(OcpiJsonOptions.V2_1_1);
    private static readonly IModuleHandler ConnectorV22 = new ModuleHandler<V2_2.Connector>(OcpiJsonOptions.V2_2);
    private static readonly IModuleHandler ConnectorV221 = new ModuleHandler<V2_2_1.Connector>(OcpiJsonOptions.V2_2_1);

    internal static IModuleHandler Connector(OcpiVersion version) => version switch
    {
        OcpiVersion.V2_0 => ConnectorV20,
        OcpiVersion.V2_1_1 => ConnectorV211,
        OcpiVersion.V2_2 => ConnectorV22,
        OcpiVersion.V2_2_1 => ConnectorV221,
        _ => throw new ArgumentOutOfRangeException(nameof(version)),
    };

    // Session
    private static readonly IModuleHandler SessionV20 = new ModuleHandler<V2_0.Session>(OcpiJsonOptions.V2_0);
    private static readonly IModuleHandler SessionV211 = new ModuleHandler<V2_1_1.Session>(OcpiJsonOptions.V2_1_1);
    private static readonly IModuleHandler SessionV22 = new ModuleHandler<V2_2.Session>(OcpiJsonOptions.V2_2);
    private static readonly IModuleHandler SessionV221 = new ModuleHandler<V2_2_1.Session>(OcpiJsonOptions.V2_2_1);

    internal static IModuleHandler Session(OcpiVersion version) => version switch
    {
        OcpiVersion.V2_0 => SessionV20,
        OcpiVersion.V2_1_1 => SessionV211,
        OcpiVersion.V2_2 => SessionV22,
        OcpiVersion.V2_2_1 => SessionV221,
        _ => throw new ArgumentOutOfRangeException(nameof(version)),
    };

    // Cdr
    private static readonly IModuleHandler CdrV20 = new ModuleHandler<V2_0.Cdr>(OcpiJsonOptions.V2_0);
    private static readonly IModuleHandler CdrV211 = new ModuleHandler<V2_1_1.Cdr>(OcpiJsonOptions.V2_1_1);
    private static readonly IModuleHandler CdrV22 = new ModuleHandler<V2_2.Cdr>(OcpiJsonOptions.V2_2);
    private static readonly IModuleHandler CdrV221 = new ModuleHandler<V2_2_1.Cdr>(OcpiJsonOptions.V2_2_1);

    internal static IModuleHandler Cdr(OcpiVersion version) => version switch
    {
        OcpiVersion.V2_0 => CdrV20,
        OcpiVersion.V2_1_1 => CdrV211,
        OcpiVersion.V2_2 => CdrV22,
        OcpiVersion.V2_2_1 => CdrV221,
        _ => throw new ArgumentOutOfRangeException(nameof(version)),
    };

    // Tariff
    private static readonly IModuleHandler TariffV20 = new ModuleHandler<V2_0.Tariff>(OcpiJsonOptions.V2_0);
    private static readonly IModuleHandler TariffV211 = new ModuleHandler<V2_1_1.Tariff>(OcpiJsonOptions.V2_1_1);
    private static readonly IModuleHandler TariffV22 = new ModuleHandler<V2_2.Tariff>(OcpiJsonOptions.V2_2);
    private static readonly IModuleHandler TariffV221 = new ModuleHandler<V2_2_1.Tariff>(OcpiJsonOptions.V2_2_1);

    internal static IModuleHandler Tariff(OcpiVersion version) => version switch
    {
        OcpiVersion.V2_0 => TariffV20,
        OcpiVersion.V2_1_1 => TariffV211,
        OcpiVersion.V2_2 => TariffV22,
        OcpiVersion.V2_2_1 => TariffV221,
        _ => throw new ArgumentOutOfRangeException(nameof(version)),
    };

    // Token
    private static readonly IModuleHandler TokenV20 = new ModuleHandler<V2_0.Token>(OcpiJsonOptions.V2_0);
    private static readonly IModuleHandler TokenV211 = new ModuleHandler<V2_1_1.Token>(OcpiJsonOptions.V2_1_1);
    private static readonly IModuleHandler TokenV22 = new ModuleHandler<V2_2.Token>(OcpiJsonOptions.V2_2);
    private static readonly IModuleHandler TokenV221 = new ModuleHandler<V2_2_1.Token>(OcpiJsonOptions.V2_2_1);

    internal static IModuleHandler Token(OcpiVersion version) => version switch
    {
        OcpiVersion.V2_0 => TokenV20,
        OcpiVersion.V2_1_1 => TokenV211,
        OcpiVersion.V2_2 => TokenV22,
        OcpiVersion.V2_2_1 => TokenV221,
        _ => throw new ArgumentOutOfRangeException(nameof(version)),
    };

    // LocationReferences
    private static readonly IModuleHandler LocRefsV20 = new ModuleHandler<V2_0.LocationReferences>(OcpiJsonOptions.V2_0);
    private static readonly IModuleHandler LocRefsV211 = new ModuleHandler<V2_1_1.LocationReferences>(OcpiJsonOptions.V2_1_1);
    private static readonly IModuleHandler LocRefsV22 = new ModuleHandler<V2_2.LocationReferences>(OcpiJsonOptions.V2_2);
    private static readonly IModuleHandler LocRefsV221 = new ModuleHandler<V2_2_1.LocationReferences>(OcpiJsonOptions.V2_2_1);

    internal static IModuleHandler LocationReferences(OcpiVersion version) => version switch
    {
        OcpiVersion.V2_0 => LocRefsV20,
        OcpiVersion.V2_1_1 => LocRefsV211,
        OcpiVersion.V2_2 => LocRefsV22,
        OcpiVersion.V2_2_1 => LocRefsV221,
        _ => throw new ArgumentOutOfRangeException(nameof(version)),
    };

    // Credentials
    private static readonly IModuleHandler CredentialsV20 = new ModuleHandler<V2_0.Credentials>(OcpiJsonOptions.V2_0);
    private static readonly IModuleHandler CredentialsV211 = new ModuleHandler<V2_1_1.Credentials>(OcpiJsonOptions.V2_1_1);
    private static readonly IModuleHandler CredentialsV22 = new ModuleHandler<V2_2.Credentials>(OcpiJsonOptions.V2_2);
    private static readonly IModuleHandler CredentialsV221 = new ModuleHandler<V2_2_1.Credentials>(OcpiJsonOptions.V2_2_1);

    internal static IModuleHandler Credentials(OcpiVersion version) => version switch
    {
        OcpiVersion.V2_0 => CredentialsV20,
        OcpiVersion.V2_1_1 => CredentialsV211,
        OcpiVersion.V2_2 => CredentialsV22,
        OcpiVersion.V2_2_1 => CredentialsV221,
        _ => throw new ArgumentOutOfRangeException(nameof(version)),
    };

    // CommandResult
    private static readonly IModuleHandler CmdResultV20 = new ModuleHandler<V2_0.CommandResponse>(OcpiJsonOptions.V2_0);
    private static readonly IModuleHandler CmdResultV211 = new ModuleHandler<V2_1_1.CommandResponse>(OcpiJsonOptions.V2_1_1);
    private static readonly IModuleHandler CmdResultV22 = new ModuleHandler<V2_2.CommandResult>(OcpiJsonOptions.V2_2);
    private static readonly IModuleHandler CmdResultV221 = new ModuleHandler<V2_2_1.CommandResult>(OcpiJsonOptions.V2_2_1);

    internal static IModuleHandler CommandResult(OcpiVersion version) => version switch
    {
        OcpiVersion.V2_0 => CmdResultV20,
        OcpiVersion.V2_1_1 => CmdResultV211,
        OcpiVersion.V2_2 => CmdResultV22,
        OcpiVersion.V2_2_1 => CmdResultV221,
        _ => throw new ArgumentOutOfRangeException(nameof(version)),
    };

    // ChargingProfileResult (2.2+ only)
    private static readonly IModuleHandler ChargingProfileResultV22 = new ModuleHandler<V2_2.ChargingProfileResult>(OcpiJsonOptions.V2_2);
    private static readonly IModuleHandler ChargingProfileResultV221 = new ModuleHandler<V2_2_1.ChargingProfileResult>(OcpiJsonOptions.V2_2_1);

    internal static IModuleHandler ChargingProfileResult(OcpiVersion version) => version switch
    {
        OcpiVersion.V2_2 => ChargingProfileResultV22,
        OcpiVersion.V2_2_1 => ChargingProfileResultV221,
        _ => throw new ArgumentOutOfRangeException(nameof(version), version,
            $"ChargingProfiles module is not available in OCPI {version.ToVersionString()}."),
    };

    // ActiveChargingProfile (2.2+ only)
    private static readonly IModuleHandler ActiveProfileV22 = new ModuleHandler<V2_2.ActiveChargingProfile>(OcpiJsonOptions.V2_2);
    private static readonly IModuleHandler ActiveProfileV221 = new ModuleHandler<V2_2_1.ActiveChargingProfile>(OcpiJsonOptions.V2_2_1);

    internal static IModuleHandler ActiveChargingProfile(OcpiVersion version) => version switch
    {
        OcpiVersion.V2_2 => ActiveProfileV22,
        OcpiVersion.V2_2_1 => ActiveProfileV221,
        _ => throw new ArgumentOutOfRangeException(nameof(version), version,
            $"ChargingProfiles module is not available in OCPI {version.ToVersionString()}."),
    };
}
