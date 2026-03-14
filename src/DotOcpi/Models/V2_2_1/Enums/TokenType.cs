using System.Diagnostics.CodeAnalysis;

namespace DotOcpi.Models.V2_2_1;

/// <summary>
/// The type of token.
/// </summary>
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "OCPI spec names")]
public enum TokenType
{
    AD_HOC_USER,
    APP_USER,
    EMAID,
    OTHER,
    RFID,
}
