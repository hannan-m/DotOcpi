using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// An Electric Vehicle Supply Equipment (EVSE) at a Location.
/// </summary>
public sealed record Evse
{
    /// <summary>Unique identifier of the EVSE within the CPO's platform (not the printed EVSE ID).</summary>
    [Required]
    [StringLength(36)]
    public required CiString Uid { get; init; }

    /// <summary>Compliant EVSE ID per DIN/ISO standard. Optional because not all EVSEs have a printed ID.</summary>
    [StringLength(48)]
    public CiString? EvseId { get; init; }

    /// <summary>Current status of the EVSE.</summary>
    [Required]
    public required Status Status { get; init; }

    /// <summary>Planned future statuses of the EVSE.</summary>
    public IReadOnlyList<StatusSchedule>? StatusSchedule { get; init; }

    /// <summary>Capabilities of this EVSE.</summary>
    public IReadOnlyList<Capability>? Capabilities { get; init; }

    /// <summary>Connectors available at this EVSE. At least one required.</summary>
    [Required]
    public required IReadOnlyList<Connector> Connectors { get; init; }

    /// <summary>Floor level (e.g. "-2", "3").</summary>
    [StringLength(4)]
    public string? FloorLevel { get; init; }

    /// <summary>Coordinates of the EVSE if different from the Location.</summary>
    public GeoLocation? Coordinates { get; init; }

    /// <summary>Physical reference number/string visible on the EVSE.</summary>
    [StringLength(16)]
    public string? PhysicalReference { get; init; }

    /// <summary>Multi-language directions to the EVSE.</summary>
    public IReadOnlyList<DisplayText>? Directions { get; init; }

    /// <summary>Parking restrictions at the EVSE.</summary>
    public IReadOnlyList<ParkingRestriction>? ParkingRestrictions { get; init; }

    /// <summary>Images of the EVSE.</summary>
    public IReadOnlyList<Image>? Images { get; init; }

    /// <summary>Timestamp when this EVSE was last updated (UTC).</summary>
    [Required]
    public required DateTimeOffset LastUpdated { get; init; }
}
