using System.Diagnostics.CodeAnalysis;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// The type of parking at a location.
/// </summary>
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "OCPI spec names")]
public enum ParkingType
{
    ALONG_MOTORWAY,
    PARKING_GARAGE,
    PARKING_LOT,
    ON_DRIVEWAY,
    ON_STREET,
    UNDERGROUND_GARAGE,
}
