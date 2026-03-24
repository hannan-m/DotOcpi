namespace DotOcpi.Security;

/// <summary>
/// Protects and unprotects OCPI tokens for secure at-rest storage.
/// Used to encrypt outbound CPO tokens (Token C) before persisting
/// them in <see cref="ITokenStore"/> and decrypt them before use
/// in outbound HTTP requests.
/// </summary>
/// <remarks>
/// <para>
/// The default <see cref="PlaintextTokenProtector"/> stores tokens
/// unencrypted — suitable for development only.
/// </para>
/// <para>
/// In ASP.NET Core deployments, <c>AddAspNetCoreServer()</c> automatically
/// registers an implementation backed by the Data Protection API.
/// Consumers can register a custom implementation via
/// <see cref="DotOcpiBuilder.AddTokenProtector{T}"/>.
/// </para>
/// </remarks>
public interface ITokenProtector
{
    /// <summary>
    /// Protects a plaintext token for secure storage.
    /// </summary>
    /// <param name="plaintext">The raw token value.</param>
    /// <returns>An opaque protected string safe for persistence.</returns>
    string Protect(string plaintext);

    /// <summary>
    /// Recovers the original token from a protected string.
    /// </summary>
    /// <param name="protectedData">The value returned by <see cref="Protect"/>.</param>
    /// <returns>The original plaintext token.</returns>
    string Unprotect(string protectedData);
}
