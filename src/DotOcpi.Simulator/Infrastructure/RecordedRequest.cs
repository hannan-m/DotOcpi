using System.Text.Json;

namespace DotOcpi.Simulator;

/// <summary>
/// A captured HTTP request made to the test CPO server.
/// Inspectable via <see cref="OcpiCpoSimulator.GetReceivedRequests"/>.
/// </summary>
public sealed class RecordedRequest
{
    /// <summary>HTTP method (GET, POST, PUT, PATCH, DELETE).</summary>
    public required string Method { get; init; }

    /// <summary>Request path (e.g., "/ocpi/locations/LOC1").</summary>
    public required string Path { get; init; }

    /// <summary>Query string (e.g., "?offset=0&amp;limit=10"), or null.</summary>
    public string? QueryString { get; init; }

    /// <summary>Request headers as key-value pairs.</summary>
    public required IReadOnlyDictionary<string, string> Headers { get; init; }

    /// <summary>Request body parsed as JSON, or null if body was empty or not JSON.</summary>
    public JsonElement? Body { get; init; }

    /// <summary>When the request was recorded (UTC).</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>HTTP response status code returned for this request.</summary>
    public int ResponseStatusCode { get; init; }
}
