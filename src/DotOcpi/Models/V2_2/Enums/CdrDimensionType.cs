using System.Diagnostics.CodeAnalysis;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// Defines the dimension type for a charging period in a CDR or session.
/// </summary>
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "OCPI spec names")]
public enum CdrDimensionType
{
    CURRENT,
    ENERGY,
    ENERGY_EXPORT,
    ENERGY_IMPORT,
    MAX_CURRENT,
    MIN_CURRENT,
    MAX_POWER,
    MIN_POWER,
    PARKING_TIME,
    POWER,
    RESERVATION_TIME,
    STATE_OF_CHARGE,
    TIME,
}
