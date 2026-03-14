namespace DotOcpi.Exceptions;

/// <summary>
/// Base exception for all OCPI library errors.
/// Reserved for truly exceptional conditions (network failures, serialization bugs).
/// Expected OCPI protocol responses use <see cref="OcpiResult{T}"/> instead.
/// </summary>
public abstract class OcpiException : Exception
{
    /// <inheritdoc/>
    protected OcpiException(string message)
        : base(message) { }

    /// <inheritdoc/>
    protected OcpiException(string message, Exception innerException)
        : base(message, innerException) { }
}
