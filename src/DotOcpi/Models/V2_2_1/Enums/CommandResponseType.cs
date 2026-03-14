using System.Diagnostics.CodeAnalysis;

namespace DotOcpi.Models.V2_2_1;

/// <summary>
/// Synchronous response to a command request (before the async result).
/// </summary>
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "OCPI spec names")]
public enum CommandResponseType
{
    NOT_SUPPORTED,
    REJECTED,
    ACCEPTED,
    UNKNOWN_SESSION,
}
