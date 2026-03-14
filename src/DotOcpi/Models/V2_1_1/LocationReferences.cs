using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_1_1;

/// <summary>
/// References to a Location and optionally specific EVSEs within it.
/// Used in authorization requests. In 2.1.1, IDs are plain strings.
/// </summary>
public sealed record LocationReferences
{
    /// <summary>Location ID.</summary>
    [Required]
    [StringLength(36)]
    public required string LocationId { get; init; }

    /// <summary>Specific EVSE UIDs at the location. Empty means any EVSE.</summary>
    public IReadOnlyList<string>? EvseUids { get; init; }
}
