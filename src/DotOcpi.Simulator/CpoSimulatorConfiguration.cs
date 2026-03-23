using DotOcpi.Simulator.Charging;
using DotOcpi.Simulator.Models;

namespace DotOcpi.Simulator;

/// <summary>
/// Configuration for <see cref="OcpiCpoSimulator"/>. Controls which
/// versions are advertised, what data is served, and failure injection.
/// </summary>
public sealed class CpoSimulatorConfiguration
{
    /// <summary>OCPI versions the test CPO supports.</summary>
    public IReadOnlyList<OcpiVersion> SupportedVersions { get; set; } = [OcpiVersion.V2_2_1];

    /// <summary>The test CPO's party identity.</summary>
    public PartyIdentity CpoIdentity { get; set; } = new("DE", "CPO");

    /// <summary>The eMSP party identity (used in token references and session data).</summary>
    public PartyIdentity EmspIdentity { get; set; } = new("NL", "MSP");

    /// <summary>
    /// Modules advertised in version detail. Null means all modules.
    /// Set to a subset to simulate a CPO that doesn't support all modules.
    /// </summary>
    public List<string>? AdvertisedModules { get; set; }

    // ── Legacy data (anonymous objects) ───────────────────────

    /// <summary>Locations to return from GET /locations (legacy anonymous object mode).</summary>
    public List<object> Locations { get; set; } = [];

    /// <summary>Tariffs to return from GET /tariffs (legacy anonymous object mode).</summary>
    public List<object> Tariffs { get; set; } = [];

    /// <summary>Sessions to return from GET /sessions (legacy anonymous object mode).</summary>
    public List<object> Sessions { get; set; } = [];

    /// <summary>CDRs to return from GET /cdrs (legacy or generated on session completion).</summary>
    public List<object> Cdrs { get; set; } = [];

    // ── Typed data (new mode) ─────────────────────────────────

    /// <summary>Typed location specifications. When set, activates typed mode with EVSE state machine and charging simulation.</summary>
    public List<LocationSpec> LocationSpecs { get; set; } = [];

    /// <summary>Typed tariff specifications.</summary>
    public List<TariffSpec> TariffSpecs { get; set; } = [];

    // ── Auth ──────────────────────────────────────────────────

    /// <summary>
    /// When true (default), module endpoints require Token B auth after registration.
    /// Auth is only enforced after a credentials exchange has issued a Token B.
    /// Set to false for tests that don't need registration.
    /// </summary>
    public bool RequireAuth { get; set; } = true;

    // ── Command response ──────────────────────────────────────

    /// <summary>OCPI status to return for command endpoints. Defaults to "ACCEPTED".</summary>
    public string CommandResponseStatus { get; set; } = "ACCEPTED";

    // ── Failure injection ─────────────────────────────────────

    /// <summary>When true, POST /credentials returns OCPI 3001 (registration rejected).</summary>
    public bool RejectRegistration { get; set; }

    /// <summary>When set, all responses use this OCPI status code instead of 1000.</summary>
    public OcpiStatusCode? ForceStatusCode { get; set; }

    /// <summary>When set, each response is delayed by this duration.</summary>
    public TimeSpan? ResponseDelay { get; set; }

    /// <summary>When true, all requests throw a TaskCanceledException (timeout simulation).</summary>
    public bool SimulateTimeout { get; set; }

    /// <summary>
    /// Per-endpoint failure overrides. Key is the module name (e.g., "locations", "tokens",
    /// "commands/START_SESSION"). Overrides take priority over the global failure settings.
    /// </summary>
    public Dictionary<string, EndpointFailureConfig> EndpointOverrides { get; set; } = new();

    // ── Command callbacks ─────────────────────────────────────

    /// <summary>When true, command endpoints POST the result back to the response_url asynchronously.</summary>
    public bool CommandCallbackEnabled { get; set; }

    /// <summary>Delay before the async command callback is fired.</summary>
    public TimeSpan CommandCallbackDelay { get; set; } = TimeSpan.FromMilliseconds(100);

    // ── Charging simulation ───────────────────────────────────

    /// <summary>Charging profile for the charging simulation (typed mode).</summary>
    public ChargingProfileSpec DefaultChargingProfile { get; set; } = ChargingProfileSpec.DcFast;

    /// <summary>Interval between charging simulation ticks (typed mode).</summary>
    public TimeSpan ChargingTickInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>When true, sessions auto-stop when target kWh is reached.</summary>
    public bool AutoStopOnTargetKwh { get; set; }

    // ── Push (webhooks) ───────────────────────────────────────

    /// <summary>Base URL of the eMSP's OCPI endpoints for push notifications.</summary>
    public string? EmspBaseUrl { get; set; }

    /// <summary>Master switch for push notifications. Default: false (backward-compat).</summary>
    public bool PushEnabled { get; set; }

    /// <summary>Push session progress to eMSP.</summary>
    public bool PushSessionUpdates { get; set; }

    /// <summary>Push CDR on session completion.</summary>
    public bool PushCdrOnCompletion { get; set; }

    // ── Authorization ─────────────────────────────────────────

    /// <summary>
    /// When true, allows callbacks to loopback/private addresses.
    /// Defaults to true since the simulator is a test utility.
    /// </summary>
    public bool AllowLoopbackCallbacks { get; set; } = true;

    /// <summary>Default result for POST /tokens/{uid}/authorize. Default: "ALLOWED".</summary>
    public string AuthorizationResult { get; set; } = "ALLOWED";

    /// <summary>Per-token authorization overrides. Key: token UID, Value: result.</summary>
    public Dictionary<string, string> TokenAuthorizations { get; set; } = new();

    // ── Pricing ───────────────────────────────────────────────

    /// <summary>ISO 4217 currency code. Default: "EUR".</summary>
    public string CpoCurrency { get; set; } = "EUR";

    /// <summary>Default price per kWh excluding VAT.</summary>
    public decimal DefaultPricePerKwh { get; set; } = 0.39m;

    /// <summary>Default VAT rate as a decimal (e.g., 0.19 for 19%).</summary>
    public decimal DefaultVatRate { get; set; } = 0.19m;

    // ── Reservations ──────────────────────────────────────────

    /// <summary>Default reservation timeout. Default: 15 minutes.</summary>
    public TimeSpan ReservationTimeout { get; set; } = TimeSpan.FromMinutes(15);

    // ── Mode detection ────────────────────────────────────────

    /// <summary>True when using legacy anonymous object mode (Locations/Tariffs Lists).</summary>
    internal bool IsLegacyMode => LocationSpecs.Count == 0;
}
