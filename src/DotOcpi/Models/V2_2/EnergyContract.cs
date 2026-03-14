using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// Information about a driver's energy contract for green energy preferences.
/// </summary>
public sealed record EnergyContract
{
    /// <summary>Name of the energy supplier.</summary>
    [Required]
    [StringLength(64)]
    public required string SupplierName { get; init; }

    /// <summary>Contract ID at the energy supplier.</summary>
    [StringLength(64)]
    public string? ContractId { get; init; }
}
