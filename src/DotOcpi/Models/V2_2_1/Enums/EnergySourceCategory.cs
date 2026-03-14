using System.Diagnostics.CodeAnalysis;

namespace DotOcpi.Models.V2_2_1;

/// <summary>
/// Category of energy source.
/// </summary>
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "OCPI spec names")]
public enum EnergySourceCategory
{
    NUCLEAR,
    GENERAL_FOSSIL,
    COAL,
    GAS,
    GENERAL_GREEN,
    SOLAR,
    WIND,
    WATER,
}
