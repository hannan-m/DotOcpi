using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// Environmental impact of the energy mix per MWh.
/// </summary>
public sealed record EnvironmentalImpact
{
    /// <summary>The category of environmental impact.</summary>
    [Required]
    public required EnvironmentalImpactCategory Category { get; init; }

    /// <summary>Amount of this impact per MWh.</summary>
    [Required]
    public required decimal Amount { get; init; }
}
