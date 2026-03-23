namespace DotOcpi.Registry;

/// <summary>
/// Represents a connection to a CPO, including negotiated version,
/// module endpoints, credential state, and party identities.
/// </summary>
public sealed record CpoConnection
{
    /// <summary>CPO's country code (ISO 3166-1 alpha-2).</summary>
    public required string CpoCountryCode { get; init; }

    /// <summary>CPO's party ID.</summary>
    public required string CpoPartyId { get; init; }

    /// <summary>eMSP country code used for this connection. Supports multi-party.</summary>
    public required string EmspCountryCode { get; init; }

    /// <summary>eMSP party ID used for this connection. Supports multi-party.</summary>
    public required string EmspPartyId { get; init; }

    /// <summary>Negotiated OCPI version for this connection.</summary>
    public required OcpiVersion Version { get; init; }

    /// <summary>CPO's module endpoints discovered during version negotiation.</summary>
    public required IReadOnlyDictionary<string, string> ModuleEndpoints { get; init; }

    /// <summary>SHA-256 hash of the Token B used for this connection.</summary>
    public required string TokenBHash { get; init; }

    /// <summary>Current status of this connection.</summary>
    public required ConnectionStatus Status { get; init; }

    /// <summary>When this connection was created (UTC).</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>When this connection was last updated (UTC).</summary>
    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>The CPO's versions endpoint URL, used for re-discovery during credential rotation.</summary>
    public string? CpoVersionsUrl { get; init; }

    /// <summary>The eMSP's versions endpoint URL, sent in credentials to this CPO.</summary>
    public string? EmspVersionsUrl { get; init; }

    /// <summary>When the last health check succeeded (UTC), or null if never.</summary>
    public DateTimeOffset? LastHealthCheckAt { get; init; }

    /// <summary>Monotonically increasing version for optimistic concurrency.</summary>
    public long ConcurrencyVersion { get; init; }

    /// <summary>
    /// Unique key for this connection: "{CpoCountryCode}:{CpoPartyId}".
    /// Computed on access — no mutable cache field that would break record equality.
    /// </summary>
    public string ConnectionKey => $"{CpoCountryCode}:{CpoPartyId}";
}
