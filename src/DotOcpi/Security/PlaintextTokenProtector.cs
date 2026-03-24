namespace DotOcpi.Security;

/// <summary>
/// No-op token protector that returns tokens unchanged.
/// Suitable for development and testing only — tokens are stored in plaintext.
/// Production deployments should use the Data Protection-backed implementation
/// (registered automatically by <c>AddAspNetCoreServer()</c>) or a custom
/// <see cref="ITokenProtector"/>.
/// </summary>
public sealed class PlaintextTokenProtector : ITokenProtector
{
    /// <inheritdoc />
    public string Protect(string plaintext) => plaintext;

    /// <inheritdoc />
    public string Unprotect(string protectedData) => protectedData;
}
