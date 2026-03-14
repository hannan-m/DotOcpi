namespace DotOcpi.Models.V2_2;

/// <summary>
/// OCPI module identifiers as used in version endpoint listings.
/// Values are lowercase to match wire format.
/// </summary>
public enum ModuleId
{
    cdrs,
    chargingprofiles,
    commands,
    credentials,
    hubclientinfo,
    locations,
    sessions,
    tariffs,
    tokens,
}
