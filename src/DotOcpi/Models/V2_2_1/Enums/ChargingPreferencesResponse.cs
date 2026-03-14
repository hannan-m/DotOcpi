using System.Diagnostics.CodeAnalysis;

namespace DotOcpi.Models.V2_2_1;

/// <summary>
/// Response to setting charging preferences on a session.
/// </summary>
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "OCPI spec names")]
public enum ChargingPreferencesResponse
{
    ACCEPTED,
    DEPARTURE_REQUIRED,
    ENERGY_NEED_REQUIRED,
    NOT_POSSIBLE,
    PROFILE_TYPE_NOT_SUPPORTED,
}
