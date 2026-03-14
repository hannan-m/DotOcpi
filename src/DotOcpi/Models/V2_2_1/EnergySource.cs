using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2_1;

/// <summary>
/// A specific energy source and its percentage in the energy mix.
/// </summary>
public sealed record EnergySource
{
    /// <summary>The type of energy source.</summary>
    [Required]
    public required EnergySourceCategory Source { get; init; }

    /// <summary>Percentage of this source in the mix (0-100).</summary>
    [Required]
    [Range(0, 100)]
    public required decimal Percentage { get; init; }
}
