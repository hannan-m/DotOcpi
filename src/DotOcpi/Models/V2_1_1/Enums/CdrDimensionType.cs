using System.Diagnostics.CodeAnalysis;

namespace DotOcpi.Models.V2_1_1;

/// <summary>
/// Defines the dimension type for a charging period in a CDR or session.
/// </summary>
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "OCPI spec names")]
public enum CdrDimensionType
{
    ENERGY,
    FLAT,
    MAX_CURRENT,
    MIN_CURRENT,
    PARKING_TIME,
    TIME,
}
