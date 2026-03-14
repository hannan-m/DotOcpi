using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_1_1;

/// <summary>
/// A token used to authorize charging sessions.
/// In 2.1.1: no country_code/party_id, uses auth_id (not contract_id),
/// no group_id, no default_profile_type, no energy_contract.
/// </summary>
public sealed record Token
{
    /// <summary>Unique token identifier.</summary>
    [Required]
    [StringLength(36)]
    public required string Uid { get; init; }

    /// <summary>Type of the token.</summary>
    [Required]
    public required TokenType Type { get; init; }

    /// <summary>Uniquely identifies the EV driver contract (called auth_id in 2.1.1).</summary>
    [Required]
    [StringLength(36)]
    public required string AuthId { get; init; }

    /// <summary>Visual number printed on the token.</summary>
    [StringLength(64)]
    public string? VisualNumber { get; init; }

    /// <summary>Token issuer name.</summary>
    [Required]
    [StringLength(64)]
    public required string Issuer { get; init; }

    /// <summary>Whether this token is valid for charging.</summary>
    [Required]
    public required bool Valid { get; init; }

    /// <summary>Whitelist authorization behavior.</summary>
    [Required]
    public required WhitelistType Whitelist { get; init; }

    /// <summary>Preferred language (ISO 639-1).</summary>
    [StringLength(2)]
    public string? Language { get; init; }

    /// <summary>Timestamp when this token was last updated (UTC).</summary>
    [Required]
    public required DateTimeOffset LastUpdated { get; init; }
}
