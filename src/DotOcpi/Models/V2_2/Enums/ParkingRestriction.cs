using System.Diagnostics.CodeAnalysis;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// Restrictions that apply to a parking spot.
/// </summary>
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "OCPI spec names")]
public enum ParkingRestriction
{
    CUSTOMERS,
    DISABLED,
    EV_ONLY,
    MOTORCYCLES,
    PLUGGED,
}
