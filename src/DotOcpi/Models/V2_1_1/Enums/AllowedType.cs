using System.Diagnostics.CodeAnalysis;

namespace DotOcpi.Models.V2_1_1;

/// <summary>
/// Whether a token is allowed to charge and the reason why (not).
/// </summary>
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "OCPI spec names")]
public enum AllowedType
{
    ALLOWED,
    BLOCKED,
    EXPIRED,
    NO_CREDIT,
    NOT_ALLOWED,
}
