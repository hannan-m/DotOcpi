using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// Charging preferences set by the driver for a session.
/// </summary>
public sealed record ChargingPreferences
{
    /// <summary>Type of charging profile the driver prefers.</summary>
    [Required]
    public required ProfileType ProfileType { get; init; }

    /// <summary>Expected departure time of the EV.</summary>
    public DateTimeOffset? DepartureTime { get; init; }

    /// <summary>Requested amount of energy in kWh.</summary>
    public decimal? EnergyNeed { get; init; }
}
