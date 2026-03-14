using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2_1;

/// <summary>
/// Token information as used in Sessions and CDRs.
/// In 2.2.1, includes country_code and party_id (not present in 2.2).
/// </summary>
public sealed record CdrToken
{
    /// <summary>ISO 3166-1 alpha-2 country code of the token owner (2.2.1 only).</summary>
    [Required]
    [StringLength(2)]
    public required CiString CountryCode { get; init; }

    /// <summary>Party ID of the token owner (2.2.1 only).</summary>
    [Required]
    [StringLength(3)]
    public required CiString PartyId { get; init; }

    /// <summary>Unique identifier of the token.</summary>
    [Required]
    [StringLength(36)]
    public required CiString Uid { get; init; }

    /// <summary>Type of the token.</summary>
    [Required]
    public required TokenType Type { get; init; }

    /// <summary>Contract ID of the token holder (renamed from auth_id in 2.2+).</summary>
    [Required]
    [StringLength(36)]
    public required CiString ContractId { get; init; }
}
