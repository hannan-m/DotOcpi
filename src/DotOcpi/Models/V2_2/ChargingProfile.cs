using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// A charging profile defining power limits over time.
/// </summary>
public sealed record ChargingProfile
{
    /// <summary>When the profile starts (UTC). Uses session start if absent.</summary>
    public DateTimeOffset? StartDateTime { get; init; }

    /// <summary>Duration of the profile in seconds.</summary>
    public int? Duration { get; init; }

    /// <summary>Unit for the limit values (W or A).</summary>
    [Required]
    public required ChargingRateUnit ChargingRateUnit { get; init; }

    /// <summary>Minimum charging rate supported by the EV.</summary>
    public decimal? MinChargingRate { get; init; }

    /// <summary>Profile periods. At least one required.</summary>
    [Required]
    public required IReadOnlyList<ChargingProfilePeriod> ChargingProfilePeriod { get; init; }
}
