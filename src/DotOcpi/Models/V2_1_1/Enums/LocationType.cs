using System.Diagnostics.CodeAnalysis;

namespace DotOcpi.Models.V2_1_1;

/// <summary>
/// The general type of the charge point location. Replaced by ParkingType in OCPI 2.2+.
/// </summary>
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "OCPI spec names")]
public enum LocationType
{
    ON_STREET,
    PARKING_GARAGE,
    UNDERGROUND_GARAGE,
    PARKING_LOT,
    OTHER,
}
