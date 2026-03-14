namespace DotOcpi.Testing;

/// <summary>
/// Configuration for <see cref="OcpiTestCpoServer"/>. Controls which
/// versions are advertised, what data is served, and failure injection.
/// </summary>
public sealed class TestCpoConfiguration
{
    /// <summary>OCPI versions the test CPO supports.</summary>
    public IReadOnlyList<OcpiVersion> SupportedVersions { get; set; } = [OcpiVersion.V2_2_1];

    /// <summary>The test CPO's party identity.</summary>
    public PartyIdentity CpoIdentity { get; set; } = new("DE", "CPO");

    /// <summary>Locations to return from GET /locations.</summary>
    public List<object> Locations { get; set; } = [];

    /// <summary>Tariffs to return from GET /tariffs.</summary>
    public List<object> Tariffs { get; set; } = [];

    /// <summary>Sessions to return from GET /sessions.</summary>
    public List<object> Sessions { get; set; } = [];

    /// <summary>CDRs to return from GET /cdrs.</summary>
    public List<object> Cdrs { get; set; } = [];

    /// <summary>When true, POST /credentials returns OCPI 3001 (registration rejected).</summary>
    public bool RejectRegistration { get; set; }

    /// <summary>When set, all responses use this OCPI status code instead of 1000.</summary>
    public OcpiStatusCode? ForceStatusCode { get; set; }

    /// <summary>When set, each response is delayed by this duration.</summary>
    public TimeSpan? ResponseDelay { get; set; }

    /// <summary>When true, all requests throw a TaskCanceledException (timeout simulation).</summary>
    public bool SimulateTimeout { get; set; }
}
