namespace DotOcpi.Exceptions;

/// <summary>
/// Thrown when HTTP communication with a CPO fails (connection refused, timeout, DNS failure).
/// </summary>
public class OcpiTransportException : OcpiException
{
    /// <inheritdoc/>
    public OcpiTransportException(string message)
        : base(message) { }

    /// <inheritdoc/>
    public OcpiTransportException(string message, Exception innerException)
        : base(message, innerException) { }
}
