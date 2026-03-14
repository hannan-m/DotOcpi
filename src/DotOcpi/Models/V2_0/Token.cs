using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_0;

/// <summary>
/// A token used to authorize charging sessions.
/// In 2.0: no last_updated timestamp and no whitelist field (WhitelistType was added in 2.1.1).
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

    /// <summary>Uniquely identifies the EV driver contract.</summary>
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

    /// <summary>Preferred language (ISO 639-1).</summary>
    [StringLength(2)]
    public string? Language { get; init; }
}
