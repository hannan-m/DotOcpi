using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// A token used to authorize charging sessions.
/// </summary>
public sealed record Token
{
    /// <summary>ISO 3166-1 alpha-2 country code of the token owner (eMSP).</summary>
    [Required]
    [StringLength(2)]
    public required CiString CountryCode { get; init; }

    /// <summary>Party ID of the token owner (eMSP).</summary>
    [Required]
    [StringLength(3)]
    public required CiString PartyId { get; init; }

    /// <summary>Unique token identifier.</summary>
    [Required]
    [StringLength(36)]
    public required CiString Uid { get; init; }

    /// <summary>Type of the token.</summary>
    [Required]
    public required TokenType Type { get; init; }

    /// <summary>Contract ID (renamed from auth_id in 2.2+).</summary>
    [Required]
    [StringLength(36)]
    public required CiString ContractId { get; init; }

    /// <summary>Visual number printed on the token.</summary>
    [StringLength(64)]
    public string? VisualNumber { get; init; }

    /// <summary>Token issuer name.</summary>
    [Required]
    [StringLength(64)]
    public required string Issuer { get; init; }

    /// <summary>Token group for fleet management.</summary>
    [StringLength(36)]
    public CiString? GroupId { get; init; }

    /// <summary>Whether this token is valid for charging.</summary>
    [Required]
    public required bool Valid { get; init; }

    /// <summary>Whitelist authorization behavior.</summary>
    [Required]
    public required WhitelistType Whitelist { get; init; }

    /// <summary>Preferred language (ISO 639-1).</summary>
    [StringLength(2)]
    public string? Language { get; init; }

    /// <summary>Default charging preference profile.</summary>
    public ProfileType? DefaultProfileType { get; init; }

    /// <summary>Driver's energy contract information.</summary>
    public EnergyContract? EnergyContract { get; init; }

    /// <summary>Timestamp when this token was last updated (UTC).</summary>
    [Required]
    public required DateTimeOffset LastUpdated { get; init; }
}
