namespace DotOcpi;

/// <summary>
/// Identifies an OCPI party by country code (ISO 3166-1 alpha-2) and party ID.
/// Used to identify both CPOs and eMSPs in OCPI communication.
/// </summary>
/// <param name="CountryCode">ISO 3166-1 alpha-2 country code (e.g., "DE", "NL").</param>
/// <param name="PartyId">CPO or eMSP identifier (max 3 characters).</param>
public readonly record struct PartyIdentity(string CountryCode, string PartyId)
{
    /// <summary>
    /// Returns a composite identifier in the format "{CC}_{PID}" (uppercased).
    /// Deterministic and suitable for dictionary keys and registry lookups.
    /// </summary>
    public string ToCompositeId() => $"{CountryCode}_{PartyId}".ToUpperInvariant();

    /// <inheritdoc/>
    public override string ToString() => ToCompositeId();
}
