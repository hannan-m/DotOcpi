using DotOcpi.Registry;

namespace DotOcpi;

/// <summary>
/// Context for an incoming OCPI request. Built by the auth filter and available to all
/// downstream handlers. Lives in the core package so consumer interfaces can reference it.
/// </summary>
/// <remarks>
/// The <c>HttpContext</c> property is added in Phase 9 when ASP.NET Core integration exists.
/// </remarks>
public sealed class OcpiRequestContext
{
    /// <summary>The full CPO connection from the registry.</summary>
    public required CpoConnection Connection { get; init; }

    /// <summary>Unique ID for this request (X-Request-ID header).</summary>
    public required string RequestId { get; init; }

    /// <summary>Correlation ID for distributed tracing (X-Correlation-ID header).</summary>
    public required string CorrelationId { get; init; }

    /// <summary>The CPO's composite identifier ("{CC}_{PID}").</summary>
    public required string CpoId { get; init; }

    /// <summary>The CPO's party identity.</summary>
    public required PartyIdentity CpoIdentity { get; init; }

    /// <summary>The eMSP party identity used for this connection.</summary>
    public required PartyIdentity EmspIdentity { get; init; }

    /// <summary>The OCPI version negotiated for this CPO connection.</summary>
    public required OcpiVersion NegotiatedVersion { get; init; }

    /// <summary>The OCPI module ID being accessed (e.g., "locations", "sessions").</summary>
    public required string ModuleId { get; init; }

    /// <summary>
    /// Returns true if the given string value is the OCPI "#NA" sentinel.
    /// </summary>
    public static bool IsFieldNotAvailable(string? value) => OcpiSentinel.IsNotAvailable(value);
}
