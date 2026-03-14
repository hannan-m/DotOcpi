using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2_1;

/// <summary>
/// Snapshot of location/EVSE/connector data at the time of the CDR.
/// Unlike Location, this is a flat structure embedded in the CDR.
/// </summary>
public sealed record CdrLocation
{
    /// <summary>Location ID.</summary>
    [Required]
    [StringLength(36)]
    public required CiString Id { get; init; }

    /// <summary>Display name of the Location.</summary>
    [StringLength(255)]
    public string? Name { get; init; }

    /// <summary>Street address.</summary>
    [Required]
    [StringLength(45)]
    public required string Address { get; init; }

    /// <summary>City or town.</summary>
    [Required]
    [StringLength(45)]
    public required string City { get; init; }

    /// <summary>Postal code. Optional in 2.2.1.</summary>
    [StringLength(10)]
    public string? PostalCode { get; init; }

    /// <summary>State or province (2.2.1 only).</summary>
    [StringLength(20)]
    public string? State { get; init; }

    /// <summary>ISO 3166-1 alpha-3 country code.</summary>
    [Required]
    [StringLength(3)]
    public required string Country { get; init; }

    /// <summary>Coordinates at the time of the CDR.</summary>
    [Required]
    public required GeoLocation Coordinates { get; init; }

    /// <summary>EVSE UID used for this CDR.</summary>
    [Required]
    [StringLength(36)]
    public required CiString EvseUid { get; init; }

    /// <summary>Compliant EVSE ID.</summary>
    [Required]
    [StringLength(48)]
    public required CiString EvseId { get; init; }

    /// <summary>Connector ID used.</summary>
    [Required]
    [StringLength(36)]
    public required CiString ConnectorId { get; init; }

    /// <summary>Connector type used.</summary>
    [Required]
    public required ConnectorType ConnectorStandard { get; init; }

    /// <summary>Connector format used.</summary>
    [Required]
    public required ConnectorFormat ConnectorFormat { get; init; }

    /// <summary>Power type used.</summary>
    [Required]
    public required PowerType ConnectorPowerType { get; init; }
}
