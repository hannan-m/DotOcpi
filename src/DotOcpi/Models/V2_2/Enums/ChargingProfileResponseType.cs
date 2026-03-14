using System.Diagnostics.CodeAnalysis;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// Synchronous response to a charging profile request.
/// </summary>
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "OCPI spec names")]
public enum ChargingProfileResponseType
{
    ACCEPTED,
    NOT_SUPPORTED,
    REJECTED,
    TOO_OFTEN,
    UNKNOWN_SESSION,
}
