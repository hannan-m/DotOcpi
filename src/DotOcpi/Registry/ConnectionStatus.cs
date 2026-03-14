namespace DotOcpi.Registry;

/// <summary>
/// Status of a CPO connection in the registry.
/// </summary>
public enum ConnectionStatus
{
    /// <summary>Registration initiated but not yet complete.</summary>
    Pending,

    /// <summary>Registration complete, credentials exchanged, connection active.</summary>
    Connected,

    /// <summary>Connection temporarily offline (health check failures).</summary>
    Offline,

    /// <summary>Connection has been unregistered. Credentials are invalidated.</summary>
    Unregistered,

    /// <summary>Registration was suspended due to repeated failures.</summary>
    Suspended,
}
