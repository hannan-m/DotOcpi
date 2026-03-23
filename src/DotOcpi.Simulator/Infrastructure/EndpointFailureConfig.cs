namespace DotOcpi.Simulator;

/// <summary>
/// Per-endpoint failure injection configuration.
/// Used as values in <see cref="CpoSimulatorConfiguration.EndpointOverrides"/>.
/// </summary>
public sealed class EndpointFailureConfig
{
    /// <summary>When set, this endpoint returns this OCPI status code instead of 1000.</summary>
    public OcpiStatusCode? ForceStatusCode { get; set; }

    /// <summary>When true, this endpoint throws a TaskCanceledException (timeout simulation).</summary>
    public bool SimulateTimeout { get; set; }

    /// <summary>When set, this endpoint is delayed by this duration.</summary>
    public TimeSpan? ResponseDelay { get; set; }
}
