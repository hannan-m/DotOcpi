using System.Diagnostics.CodeAnalysis;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// Defines when a token may be used for charging without real-time authorization.
/// </summary>
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "OCPI spec names")]
public enum WhitelistType
{
    ALWAYS,
    ALLOWED,
    ALLOWED_OFFLINE,
    NEVER,
}
