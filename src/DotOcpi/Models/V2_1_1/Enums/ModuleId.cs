namespace DotOcpi.Models.V2_1_1;

/// <summary>
/// OCPI module identifiers as used in version endpoint listings.
/// Values are lowercase to match wire format. No chargingprofiles or hubclientinfo in 2.1.1.
/// </summary>
public enum ModuleId
{
    cdrs,
    commands,
    credentials,
    locations,
    sessions,
    tariffs,
    tokens,
}
