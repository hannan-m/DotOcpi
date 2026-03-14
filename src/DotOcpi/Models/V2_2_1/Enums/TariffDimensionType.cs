using System.Diagnostics.CodeAnalysis;

namespace DotOcpi.Models.V2_2_1;

/// <summary>
/// Defines the dimension type for tariff price components.
/// Note: FLAT is not available in 2.2+; use TariffElement with no restrictions instead.
/// </summary>
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "OCPI spec names")]
public enum TariffDimensionType
{
    ENERGY,
    PARKING_TIME,
    TIME,
}
