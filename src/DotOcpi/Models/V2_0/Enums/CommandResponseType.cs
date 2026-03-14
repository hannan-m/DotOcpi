using System.Diagnostics.CodeAnalysis;

namespace DotOcpi.Models.V2_0;

/// <summary>
/// Response to a command request. In 2.0 this is used for both synchronous and asynchronous
/// command responses (there is no separate CommandResultType).
/// </summary>
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "OCPI spec names")]
public enum CommandResponseType
{
    NOT_SUPPORTED,
    REJECTED,
    ACCEPTED,
    TIMEOUT,
    UNKNOWN_SESSION,
}
