using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2_1;

/// <summary>
/// The currently active charging profile on a session.
/// </summary>
public sealed record ActiveChargingProfile
{
    /// <summary>When this profile became active (UTC).</summary>
    [Required]
    public required DateTimeOffset StartDateTime { get; init; }

    /// <summary>The active charging profile.</summary>
    [Required]
    public required ChargingProfile ChargingProfile { get; init; }
}
