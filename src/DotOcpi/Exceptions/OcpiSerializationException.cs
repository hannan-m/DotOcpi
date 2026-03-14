namespace DotOcpi.Exceptions;

/// <summary>
/// Thrown when JSON serialization or deserialization of an OCPI message fails
/// (malformed JSON, type mismatch, missing required fields).
/// </summary>
public class OcpiSerializationException : OcpiException
{
    /// <inheritdoc/>
    public OcpiSerializationException(string message)
        : base(message) { }

    /// <inheritdoc/>
    public OcpiSerializationException(string message, Exception innerException)
        : base(message, innerException) { }
}
