using System.Diagnostics.CodeAnalysis;

namespace DotOcpi.Models.V2_2_1;

/// <summary>
/// Type of tariff, used to distinguish between ad-hoc payments and profile-based tariffs.
/// </summary>
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "OCPI spec names")]
public enum TariffType
{
    AD_HOC_PAYMENT,
    PROFILE_CHEAP,
    PROFILE_FAST,
    PROFILE_GREEN,
    REGULAR,
}
