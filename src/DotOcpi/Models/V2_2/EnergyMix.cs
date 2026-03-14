using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// Energy mix information at a Location or Tariff level.
/// </summary>
public sealed record EnergyMix
{
    /// <summary>Whether the energy is 100% from renewable sources.</summary>
    [Required]
    public required bool IsGreenEnergy { get; init; }

    /// <summary>Breakdown of energy sources.</summary>
    public IReadOnlyList<EnergySource>? EnergySources { get; init; }

    /// <summary>Environmental impact values.</summary>
    public IReadOnlyList<EnvironmentalImpact>? EnvironImpact { get; init; }

    /// <summary>Name of the energy supplier.</summary>
    [StringLength(64)]
    public string? SupplierName { get; init; }

    /// <summary>Name of the energy product.</summary>
    [StringLength(64)]
    public string? EnergyProductName { get; init; }
}
