using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// Command to start a charging session.
/// </summary>
public sealed record StartSession
{
    /// <summary>HTTPS URL for the async result callback.</summary>
    [Required]
    [StringLength(255)]
    public required string ResponseUrl { get; init; }

    /// <summary>Token to use for this session.</summary>
    [Required]
    public required Token Token { get; init; }

    /// <summary>Location where the session should start.</summary>
    [Required]
    [StringLength(36)]
    public required CiString LocationId { get; init; }

    /// <summary>Specific EVSE to use (optional).</summary>
    [StringLength(36)]
    public CiString? EvseUid { get; init; }

    /// <summary>Reference from prior authorization (2.2+ only).</summary>
    [StringLength(36)]
    public CiString? AuthorizationReference { get; init; }
}
