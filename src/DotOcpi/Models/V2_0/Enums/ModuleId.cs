namespace DotOcpi.Models.V2_0;

/// <summary>
/// OCPI module identifiers as used in version endpoint listings.
/// Values are lowercase to match wire format.
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
