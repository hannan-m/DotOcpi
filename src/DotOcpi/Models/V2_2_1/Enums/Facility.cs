using System.Diagnostics.CodeAnalysis;

namespace DotOcpi.Models.V2_2_1;

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
    BIKE_SHARING,
    BUS_STOP,
    TAXI_STAND,
    TRAM_STOP,
    METRO_STATION,
    TRAIN_STATION,
    AIRPORT,
    PARKING_LOT,
    CARPOOL_PARKING,
    FUEL_STATION,
    WIFI,
}
