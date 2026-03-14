using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// References to a Location and optionally specific EVSEs within it.
/// Used in authorization requests.
/// </summary>
public sealed record LocationReferences
{
    /// <summary>Location ID.</summary>
    [Required]
    [StringLength(36)]
    public required CiString LocationId { get; init; }

    /// <summary>Specific EVSE UIDs at the location. Empty means any EVSE.</summary>
    public IReadOnlyList<CiString>? EvseUids { get; init; }
}
