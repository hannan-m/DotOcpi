using System.Diagnostics.CodeAnalysis;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// Category of environmental impact.
/// </summary>
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "OCPI spec names")]
public enum EnvironmentalImpactCategory
{
    NUCLEAR_WASTE,
    CARBON_DIOXIDE,
}
