using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// Token information as used in Sessions and CDRs.
/// </summary>
public sealed record CdrToken
{
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
