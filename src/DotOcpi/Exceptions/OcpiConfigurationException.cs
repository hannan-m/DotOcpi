namespace DotOcpi.Exceptions;

/// <summary>
/// Thrown when the library is misconfigured (HTTP URL where HTTPS required,
/// missing required options, Token A in file-based config).
/// </summary>
public class OcpiConfigurationException : OcpiException
{
    /// <inheritdoc/>
    public OcpiConfigurationException(string message)
        : base(message) { }

    /// <inheritdoc/>
    public OcpiConfigurationException(string message, Exception innerException)
        : base(message, innerException) { }
}
