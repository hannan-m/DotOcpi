using System.Diagnostics.CodeAnalysis;

namespace DotOcpi.Models.V2_1_1;

/// <summary>
/// Method used to authenticate/authorize a charging session.
/// </summary>
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "OCPI spec names")]
public enum AuthMethod
{
    AUTH_REQUEST,
    WHITELIST,
}
