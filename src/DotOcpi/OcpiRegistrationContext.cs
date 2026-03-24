using DotOcpi.Security;

namespace DotOcpi;

/// <summary>
/// Context for an incoming OCPI registration request (POST /credentials with Token A).
/// Used before any <see cref="Registry.CpoConnection"/> exists — the connection is
/// created as a result of the registration handshake, not before it.
/// </summary>
public sealed class OcpiRegistrationContext
{
    /// <summary>Unique ID for this request (X-Request-ID header).</summary>
    public required string RequestId { get; init; }

    /// <summary>Correlation ID for distributed tracing (X-Correlation-ID header).</summary>
    public required string CorrelationId { get; init; }

    /// <summary>
    /// The OCPI version from the URL path (e.g., /ocpi/2.2.1/credentials).
    /// Not yet "negotiated" in the OCPI sense — it is the version the CPO chose to POST to.
    /// </summary>
    public required OcpiVersion Version { get; init; }

    /// <summary>
    /// The validated Token A entry from the token store.
    /// Contains the token hash and the party ID the token was registered for.
    /// </summary>
    public required TokenEntry TokenAEntry { get; init; }
}
