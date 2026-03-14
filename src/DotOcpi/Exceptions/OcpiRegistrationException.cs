namespace DotOcpi.Exceptions;

/// <summary>
/// Thrown when the OCPI registration/credentials handshake fails
/// (no common version, CPO rejects credentials, Token A already consumed).
/// </summary>
public class OcpiRegistrationException : OcpiException
{
    /// <inheritdoc/>
    public OcpiRegistrationException(string message)
        : base(message) { }

    /// <inheritdoc/>
    public OcpiRegistrationException(string message, Exception innerException)
        : base(message, innerException) { }
}
