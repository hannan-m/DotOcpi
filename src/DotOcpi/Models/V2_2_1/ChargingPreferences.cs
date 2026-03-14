using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2_1;

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

    /// <summary>Whether the EV may discharge energy back to the grid (2.2.1 only).</summary>
    public bool? DischargeAllowed { get; init; }
}
