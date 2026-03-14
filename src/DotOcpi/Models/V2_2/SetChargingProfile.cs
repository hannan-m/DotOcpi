using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// Request to set a charging profile on a session.
/// </summary>
public sealed record SetChargingProfile
{
    /// <summary>The charging profile to set.</summary>
    [Required]
    public required ChargingProfile ChargingProfile { get; init; }

    /// <summary>HTTPS URL for the async result callback.</summary>
    [Required]
    [StringLength(255)]
    public required string ResponseUrl { get; init; }
}
