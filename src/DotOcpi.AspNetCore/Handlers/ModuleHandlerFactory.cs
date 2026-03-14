using DotOcpi.Serialization;
using V2_0 = DotOcpi.Models.V2_0;
using V2_1_1 = DotOcpi.Models.V2_1_1;
using V2_2 = DotOcpi.Models.V2_2;
using V2_2_1 = DotOcpi.Models.V2_2_1;

namespace DotOcpi.AspNetCore.Handlers;

/// <summary>
/// Creates version-specific module handlers for OCPI request body deserialization.
/// Uses the strategy pattern: selects the correct deserializer based on the
/// negotiated OCPI version for each CPO connection.
/// </summary>
internal static class ModuleHandlerFactory
{
    /// <summary>Creates a handler for Location deserialization.</summary>
    internal static IModuleHandler Location(OcpiVersion version) =>
        version switch
        {
            OcpiVersion.V2_0 => new ModuleHandler<V2_0.Location>(OcpiJsonOptions.V2_0),
            OcpiVersion.V2_1_1 => new ModuleHandler<V2_1_1.Location>(OcpiJsonOptions.V2_1_1),
            OcpiVersion.V2_2 => new ModuleHandler<V2_2.Location>(OcpiJsonOptions.V2_2),
            OcpiVersion.V2_2_1 => new ModuleHandler<V2_2_1.Location>(OcpiJsonOptions.V2_2_1),
            _ => throw new ArgumentOutOfRangeException(nameof(version)),
        };

    /// <summary>Creates a handler for EVSE deserialization.</summary>
    internal static IModuleHandler Evse(OcpiVersion version) =>
        version switch
        {
            OcpiVersion.V2_0 => new ModuleHandler<V2_0.Evse>(OcpiJsonOptions.V2_0),
            OcpiVersion.V2_1_1 => new ModuleHandler<V2_1_1.Evse>(OcpiJsonOptions.V2_1_1),
            OcpiVersion.V2_2 => new ModuleHandler<V2_2.Evse>(OcpiJsonOptions.V2_2),
            OcpiVersion.V2_2_1 => new ModuleHandler<V2_2_1.Evse>(OcpiJsonOptions.V2_2_1),
            _ => throw new ArgumentOutOfRangeException(nameof(version)),
        };

    /// <summary>Creates a handler for Connector deserialization.</summary>
    internal static IModuleHandler Connector(OcpiVersion version) =>
        version switch
        {
            OcpiVersion.V2_0 => new ModuleHandler<V2_0.Connector>(OcpiJsonOptions.V2_0),
            OcpiVersion.V2_1_1 => new ModuleHandler<V2_1_1.Connector>(OcpiJsonOptions.V2_1_1),
            OcpiVersion.V2_2 => new ModuleHandler<V2_2.Connector>(OcpiJsonOptions.V2_2),
            OcpiVersion.V2_2_1 => new ModuleHandler<V2_2_1.Connector>(OcpiJsonOptions.V2_2_1),
            _ => throw new ArgumentOutOfRangeException(nameof(version)),
        };

    /// <summary>Creates a handler for Session deserialization.</summary>
    internal static IModuleHandler Session(OcpiVersion version) =>
        version switch
        {
            OcpiVersion.V2_0 => new ModuleHandler<V2_0.Session>(OcpiJsonOptions.V2_0),
            OcpiVersion.V2_1_1 => new ModuleHandler<V2_1_1.Session>(OcpiJsonOptions.V2_1_1),
            OcpiVersion.V2_2 => new ModuleHandler<V2_2.Session>(OcpiJsonOptions.V2_2),
            OcpiVersion.V2_2_1 => new ModuleHandler<V2_2_1.Session>(OcpiJsonOptions.V2_2_1),
            _ => throw new ArgumentOutOfRangeException(nameof(version)),
        };

    /// <summary>Creates a handler for CDR deserialization.</summary>
    internal static IModuleHandler Cdr(OcpiVersion version) =>
        version switch
        {
            OcpiVersion.V2_0 => new ModuleHandler<V2_0.Cdr>(OcpiJsonOptions.V2_0),
            OcpiVersion.V2_1_1 => new ModuleHandler<V2_1_1.Cdr>(OcpiJsonOptions.V2_1_1),
            OcpiVersion.V2_2 => new ModuleHandler<V2_2.Cdr>(OcpiJsonOptions.V2_2),
            OcpiVersion.V2_2_1 => new ModuleHandler<V2_2_1.Cdr>(OcpiJsonOptions.V2_2_1),
            _ => throw new ArgumentOutOfRangeException(nameof(version)),
        };

    /// <summary>Creates a handler for Tariff deserialization.</summary>
    internal static IModuleHandler Tariff(OcpiVersion version) =>
        version switch
        {
            OcpiVersion.V2_0 => new ModuleHandler<V2_0.Tariff>(OcpiJsonOptions.V2_0),
            OcpiVersion.V2_1_1 => new ModuleHandler<V2_1_1.Tariff>(OcpiJsonOptions.V2_1_1),
            OcpiVersion.V2_2 => new ModuleHandler<V2_2.Tariff>(OcpiJsonOptions.V2_2),
            OcpiVersion.V2_2_1 => new ModuleHandler<V2_2_1.Tariff>(OcpiJsonOptions.V2_2_1),
            _ => throw new ArgumentOutOfRangeException(nameof(version)),
        };

    /// <summary>Creates a handler for Token deserialization.</summary>
    internal static IModuleHandler Token(OcpiVersion version) =>
        version switch
        {
            OcpiVersion.V2_0 => new ModuleHandler<V2_0.Token>(OcpiJsonOptions.V2_0),
            OcpiVersion.V2_1_1 => new ModuleHandler<V2_1_1.Token>(OcpiJsonOptions.V2_1_1),
            OcpiVersion.V2_2 => new ModuleHandler<V2_2.Token>(OcpiJsonOptions.V2_2),
            OcpiVersion.V2_2_1 => new ModuleHandler<V2_2_1.Token>(OcpiJsonOptions.V2_2_1),
            _ => throw new ArgumentOutOfRangeException(nameof(version)),
        };

    /// <summary>Creates a handler for LocationReferences deserialization (token authorize requests).</summary>
    internal static IModuleHandler LocationReferences(OcpiVersion version) =>
        version switch
        {
            OcpiVersion.V2_0 => new ModuleHandler<V2_0.LocationReferences>(OcpiJsonOptions.V2_0),
            OcpiVersion.V2_1_1 => new ModuleHandler<V2_1_1.LocationReferences>(OcpiJsonOptions.V2_1_1),
            OcpiVersion.V2_2 => new ModuleHandler<V2_2.LocationReferences>(OcpiJsonOptions.V2_2),
            OcpiVersion.V2_2_1 => new ModuleHandler<V2_2_1.LocationReferences>(OcpiJsonOptions.V2_2_1),
            _ => throw new ArgumentOutOfRangeException(nameof(version)),
        };

    /// <summary>
    /// Creates a handler for command result deserialization.
    /// Uses CommandResponse for 2.0/2.1.1 and CommandResult for 2.2+.
    /// </summary>
    internal static IModuleHandler CommandResult(OcpiVersion version) =>
        version switch
        {
            OcpiVersion.V2_0 => new ModuleHandler<V2_0.CommandResponse>(OcpiJsonOptions.V2_0),
            OcpiVersion.V2_1_1 => new ModuleHandler<V2_1_1.CommandResponse>(OcpiJsonOptions.V2_1_1),
            OcpiVersion.V2_2 => new ModuleHandler<V2_2.CommandResult>(OcpiJsonOptions.V2_2),
            OcpiVersion.V2_2_1 => new ModuleHandler<V2_2_1.CommandResult>(OcpiJsonOptions.V2_2_1),
            _ => throw new ArgumentOutOfRangeException(nameof(version)),
        };

    /// <summary>
    /// Creates a handler for ChargingProfileResult deserialization.
    /// Only available for 2.2+; throws for earlier versions.
    /// </summary>
    internal static IModuleHandler ChargingProfileResult(OcpiVersion version) =>
        version switch
        {
            OcpiVersion.V2_2 => new ModuleHandler<V2_2.ChargingProfileResult>(OcpiJsonOptions.V2_2),
            OcpiVersion.V2_2_1 => new ModuleHandler<V2_2_1.ChargingProfileResult>(OcpiJsonOptions.V2_2_1),
            _ => throw new ArgumentOutOfRangeException(
                nameof(version),
                version,
                $"ChargingProfiles module is not available in OCPI {version.ToVersionString()}."
            ),
        };

    /// <summary>
    /// Creates a handler for ActiveChargingProfile deserialization.
    /// Only available for 2.2+; throws for earlier versions.
    /// </summary>
    internal static IModuleHandler ActiveChargingProfile(OcpiVersion version) =>
        version switch
        {
            OcpiVersion.V2_2 => new ModuleHandler<V2_2.ActiveChargingProfile>(OcpiJsonOptions.V2_2),
            OcpiVersion.V2_2_1 => new ModuleHandler<V2_2_1.ActiveChargingProfile>(OcpiJsonOptions.V2_2_1),
            _ => throw new ArgumentOutOfRangeException(
                nameof(version),
                version,
                $"ChargingProfiles module is not available in OCPI {version.ToVersionString()}."
            ),
        };
}
