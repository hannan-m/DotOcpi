using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// A period within a charging profile with a power limit.
/// </summary>
public sealed record ChargingProfilePeriod
{
    /// <summary>Offset from the profile start in seconds.</summary>
    [Required]
    public required int StartPeriod { get; init; }

    /// <summary>Power limit in W or A (depending on charging_rate_unit).</summary>
    [Required]
    public required decimal Limit { get; init; }

    /// <summary>Number of phases to use for charging.</summary>
    public int? NumberPhases { get; init; }
}
