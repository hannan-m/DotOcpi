namespace DotOcpi;

/// <summary>
/// Identifies an OCPI party by country code (ISO 3166-1 alpha-2) and party ID.
/// Used to identify both CPOs and eMSPs in OCPI communication.
/// </summary>
/// <param name="CountryCode">ISO 3166-1 alpha-2 country code (e.g., "DE", "NL").</param>
/// <param name="PartyId">CPO or eMSP identifier (max 3 characters).</param>
public readonly record struct PartyIdentity
{
    public string CountryCode { get; }
    public string PartyId { get; }

    public PartyIdentity(string CountryCode, string PartyId)
    {
        ArgumentNullException.ThrowIfNull(CountryCode);
        ArgumentNullException.ThrowIfNull(PartyId);
        this.CountryCode = CountryCode;
        this.PartyId = PartyId;
    }

    /// <summary>
    /// Returns a composite identifier in the format "{CC}_{PID}" (uppercased).
    /// Deterministic and suitable for dictionary keys and registry lookups.
    /// </summary>
    public string ToCompositeId() =>
        string.Create(
            CountryCode.Length + 1 + PartyId.Length,
            (CountryCode, PartyId),
            static (span, state) =>
            {
                state.CountryCode.AsSpan().ToUpperInvariant(span);
                span[state.CountryCode.Length] = '_';
                state.PartyId.AsSpan().ToUpperInvariant(span[(state.CountryCode.Length + 1)..]);
            }
        );

    /// <inheritdoc/>
    public override string ToString() => ToCompositeId();
}
