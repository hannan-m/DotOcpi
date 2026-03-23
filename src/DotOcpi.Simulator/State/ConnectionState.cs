namespace DotOcpi.Simulator.State;

/// <summary>
/// Tracks per-connection state: negotiated version, tokens, and eMSP endpoints.
/// Each credentials exchange creates or updates a connection.
/// </summary>
internal sealed class ConnectionState
{
    private readonly object _lock = new();

    public required string ConnectionId { get; init; }
    public OcpiVersion NegotiatedVersion { get; set; }
    public string? IssuedTokenB { get; set; }
    public string? ReceivedTokenC { get; set; }
    public string? EmspVersionsUrl { get; set; }
    public Dictionary<string, string> EmspModuleEndpoints { get; } = new();
    public DateTimeOffset? RegisteredAt { get; set; }

    /// <summary>Atomically sets the negotiated version.</summary>
    public void SetNegotiatedVersion(OcpiVersion version)
    {
        lock (_lock)
            NegotiatedVersion = version;
    }

    /// <summary>Atomically sets tokens and registration state.</summary>
    public void SetRegistration(string tokenB, string? tokenC, string? emspVersionsUrl)
    {
        lock (_lock)
        {
            IssuedTokenB = tokenB;
            ReceivedTokenC = tokenC;
            EmspVersionsUrl = emspVersionsUrl;
            RegisteredAt = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>Atomically clears registration state.</summary>
    public void ClearRegistration()
    {
        lock (_lock)
        {
            IssuedTokenB = null;
            ReceivedTokenC = null;
        }
    }
}
