using System.Diagnostics.CodeAnalysis;

namespace DotOcpi.Models.V2_1_1;

/// <summary>
/// Facilities available at a location.
/// </summary>
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "OCPI spec names")]
public enum Facility
{
    HOTEL,
    RESTAURANT,
    CAFE,
    MALL,
    SUPERMARKET,
    SPORT,
    RECREATION_AREA,
    NATURE,
    MUSEUM,
    BUS_STOP,
    TAXI_STAND,
    TRAIN_STATION,
    AIRPORT,
    CARPOOL_PARKING,
    FUEL_STATION,
    WIFI,
}
