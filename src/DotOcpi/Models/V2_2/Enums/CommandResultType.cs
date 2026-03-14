using System.Diagnostics.CodeAnalysis;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// Asynchronous result of a command execution posted to the response_url.
/// </summary>
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "OCPI spec names")]
public enum CommandResultType
{
    ACCEPTED,
    CANCELED_RESERVATION,
    EVSE_OCCUPIED,
    EVSE_INOPERATIVE,
    FAILED,
    NOT_SUPPORTED,
    REJECTED,
    TIMEOUT,
    UNKNOWN_RESERVATION,
}
