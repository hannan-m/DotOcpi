using System.Diagnostics.CodeAnalysis;

namespace DotOcpi.Models.V2_0;

/// <summary>
/// Defines the dimension type for tariff price components.
/// </summary>
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "OCPI spec names")]
public enum TariffDimensionType
{
    ENERGY,
    FLAT,
    PARKING_TIME,
    TIME,
}
