using System.Diagnostics.CodeAnalysis;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// Charging preference profile type selected by the driver.
/// </summary>
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "OCPI spec names")]
public enum ProfileType
{
    CHEAP,
    FAST,
    GREEN,
    REGULAR,
}
