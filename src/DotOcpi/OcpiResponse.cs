namespace DotOcpi;

/// <summary>
/// The standard OCPI response envelope sent on the wire.
/// Every OCPI response wraps data in this structure.
/// </summary>
/// <typeparam name="T">The type of the data payload.</typeparam>
public sealed class OcpiResponse<T>
{
    /// <summary>The data payload. Null when the response indicates an error.</summary>
    public T? Data { get; init; }

    /// <summary>OCPI status code (1xxx success, 2xxx client error, 3xxx server error).</summary>
    public int StatusCode { get; init; }

    /// <summary>Optional human-readable status message.</summary>
    public string? StatusMessage { get; init; }

    /// <summary>Timestamp of the response.</summary>
    public DateTimeOffset Timestamp { get; init; }
}
