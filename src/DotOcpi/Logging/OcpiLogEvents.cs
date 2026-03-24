namespace DotOcpi.Logging;

/// <summary>
/// Event ID constants for structured log entries. Each range maps to a
/// specific subsystem so log entries can be filtered or searched by ID.
/// </summary>
public static class OcpiLogEvents
{
    // ── Registration (1000–1099) ────────────────────────────────────────

    /// <summary>Registration handshake started.</summary>
    public const int HandshakeStarted = 1001;

    /// <summary>OCPI version successfully negotiated.</summary>
    public const int VersionNegotiated = 1002;

    /// <summary>Registration handshake completed successfully.</summary>
    public const int RegistrationComplete = 1003;

    /// <summary>Registration handshake failed.</summary>
    public const int HandshakeFailed = 1004;

    /// <summary>Token rotation (Token B → Token C) completed.</summary>
    public const int TokenRotationComplete = 1005;

    /// <summary>CPO unregistered successfully.</summary>
    public const int Unregistered = 1006;

    // ── Authentication (2000–2099) ──────────────────────────────────────

    /// <summary>Incoming request authenticated successfully.</summary>
    public const int AuthSuccess = 2001;

    /// <summary>Request missing Authorization header or token.</summary>
    public const int AuthMissingToken = 2002;

    /// <summary>Token present but hash does not match any stored token.</summary>
    public const int AuthInvalidToken = 2003;

    /// <summary>Token matched but is expired or revoked.</summary>
    public const int AuthTokenExpired = 2004;

    /// <summary>Token matched but CPO not found in registry.</summary>
    public const int AuthCpoNotFound = 2005;

    /// <summary>Token purpose mismatch (e.g., Token B used for registration).</summary>
    public const int AuthWrongPurpose = 2006;

    // ── Client / Outbound (3000–3099) ───────────────────────────────────

    /// <summary>Outbound OCPI request sent to CPO.</summary>
    public const int RequestSent = 3001;

    /// <summary>Response received from CPO.</summary>
    public const int ResponseReceived = 3002;

    /// <summary>Outbound request failed (transport or protocol error).</summary>
    public const int RequestFailed = 3003;

    /// <summary>Pagination: fetched next page.</summary>
    public const int PaginationPage = 3004;

    /// <summary>Circuit breaker tripped for a CPO endpoint.</summary>
    public const int CircuitBreakerTripped = 3005;

    // ── Server / Inbound (4000–4099) ────────────────────────────────────

    /// <summary>Inbound OCPI request received from CPO.</summary>
    public const int InboundRequestReceived = 4001;

    /// <summary>Inbound request processed successfully.</summary>
    public const int InboundRequestProcessed = 4002;

    /// <summary>Inbound request rejected (validation, version mismatch, etc.).</summary>
    public const int InboundRequestRejected = 4003;

    /// <summary>Consumer handler threw an unhandled exception.</summary>
    public const int ConsumerHandlerFailed = 4004;

    // ── Registry (5000–5099) ────────────────────────────────────────────

    /// <summary>CPO added to registry.</summary>
    public const int CpoAdded = 5001;

    /// <summary>CPO entry updated in registry.</summary>
    public const int CpoUpdated = 5002;

    /// <summary>CPO removed from registry.</summary>
    public const int CpoRemoved = 5003;

    /// <summary>Registry cache hit.</summary>
    public const int CacheHit = 5004;

    /// <summary>Registry cache miss.</summary>
    public const int CacheMiss = 5005;

    /// <summary>Registry cache invalidated.</summary>
    public const int CacheInvalidated = 5006;

    // ── Sync (6000–6099) ────────────────────────────────────────────────

    /// <summary>Pull sync cycle started for a CPO.</summary>
    public const int SyncStarted = 6001;

    /// <summary>Pull sync page fetched.</summary>
    public const int SyncPageFetched = 6002;

    /// <summary>Pull sync cycle completed successfully.</summary>
    public const int SyncComplete = 6003;

    /// <summary>Pull sync cycle failed.</summary>
    public const int SyncFailed = 6004;

    // ── Health (7000–7099) ──────────────────────────────────────────────

    /// <summary>Health probe succeeded.</summary>
    public const int ProbeSuccess = 7001;

    /// <summary>Health probe failed.</summary>
    public const int ProbeFailed = 7002;

    /// <summary>CPO marked as offline after probe failures.</summary>
    public const int CpoMarkedOffline = 7003;

    /// <summary>CPO recovered from offline status.</summary>
    public const int CpoRecovered = 7004;

    // ── Validation (8000–8099) ──────────────────────────────────────────

    /// <summary>Model validation failed.</summary>
    public const int ValidationFailed = 8001;

    /// <summary>Version mismatch between request and connection.</summary>
    public const int VersionMismatch = 8002;

    /// <summary>URL path does not match request body identifiers.</summary>
    public const int BodyPathMismatch = 8003;
}
