using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_0;

/// <summary>
/// References to a Location and optionally specific EVSEs and Connectors within it.
/// Used in authorization requests. In 2.0, includes connector_ids (removed in 2.2+).
/// </summary>
public sealed record LocationReferences
{
    /// <summary>Location ID.</summary>
    [Required]
    [StringLength(36)]
    public required string LocationId { get; init; }

    /// <summary>Specific EVSE UIDs at the location. Empty means any EVSE.</summary>
    public IReadOnlyList<string>? EvseUids { get; init; }

    /// <summary>Specific Connector IDs. Present in 2.0 (removed in 2.2+).</summary>
    public IReadOnlyList<string>? ConnectorIds { get; init; }
}
