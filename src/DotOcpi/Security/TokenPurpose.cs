namespace DotOcpi.Security;

/// <summary>
/// Identifies the purpose of a stored token hash.
/// </summary>
public enum TokenPurpose
{
    /// <summary>Pre-shared token used only during initial registration. Consumed once.</summary>
    TokenA,

    /// <summary>Token assigned during credentials handshake. Used for ongoing communication.</summary>
    TokenB,

    /// <summary>Token issued during credential rotation. Replaces Token B.</summary>
    TokenC,
}
