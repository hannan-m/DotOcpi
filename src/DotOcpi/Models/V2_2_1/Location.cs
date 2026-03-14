using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2_1;

/// <summary>
/// A charging location with one or more EVSEs.
/// </summary>
public sealed record Location
{
    /// <summary>ISO 3166-1 alpha-2 country code of the CPO that owns this Location.</summary>
    [Required]
    [StringLength(2)]
    public required CiString CountryCode { get; init; }

    /// <summary>CPO ID of the CPO that owns this Location.</summary>
    [Required]
    [StringLength(3)]
    public required CiString PartyId { get; init; }

    /// <summary>Unique identifier of the Location within the CPO's platform.</summary>
    [Required]
    [StringLength(36)]
    public required CiString Id { get; init; }

    /// <summary>Whether this Location may be published (shown to EV drivers).</summary>
    [Required]
    public required bool Publish { get; init; }

    /// <summary>
    /// When <see cref="Publish"/> is false, only tokens matching these entries can see this Location.
    /// </summary>
    public IReadOnlyList<PublishTokenType>? PublishAllowedTo { get; init; }

    /// <summary>Display name of the Location.</summary>
    [StringLength(255)]
    public string? Name { get; init; }

    /// <summary>Street/block address of the Location.</summary>
    [Required]
    [StringLength(45)]
    public required string Address { get; init; }

    /// <summary>City or town.</summary>
    [Required]
    [StringLength(45)]
    public required string City { get; init; }

    /// <summary>Postal code. Optional in 2.2+.</summary>
    [StringLength(10)]
    public string? PostalCode { get; init; }

    /// <summary>State or province.</summary>
    [StringLength(20)]
    public string? State { get; init; }

    /// <summary>ISO 3166-1 alpha-3 country code.</summary>
    [Required]
    [StringLength(3)]
    public required string Country { get; init; }

    /// <summary>Coordinates of the Location.</summary>
    [Required]
    public required GeoLocation Coordinates { get; init; }

    /// <summary>Additional geo locations (e.g. entrance, exit).</summary>
    public IReadOnlyList<AdditionalGeoLocation>? RelatedLocations { get; init; }

    /// <summary>Type of parking at the Location.</summary>
    public ParkingType? ParkingType { get; init; }

    /// <summary>EVSEs at this Location.</summary>
    public IReadOnlyList<Evse>? Evses { get; init; }

    /// <summary>Human-readable directions to the Location.</summary>
    public IReadOnlyList<DisplayText>? Directions { get; init; }

    /// <summary>Business details of the operator.</summary>
    public BusinessDetails? Operator { get; init; }

    /// <summary>Business details of the suboperator.</summary>
    public BusinessDetails? Suboperator { get; init; }

    /// <summary>Business details of the owner.</summary>
    public BusinessDetails? Owner { get; init; }

    /// <summary>Facilities available at or near the Location.</summary>
    public IReadOnlyList<Facility>? Facilities { get; init; }

    /// <summary>IANA time zone of the Location. Required in 2.2+.</summary>
    [Required]
    [StringLength(255)]
    public required string TimeZone { get; init; }

    /// <summary>Hours of operation.</summary>
    public Hours? OpeningTimes { get; init; }

    /// <summary>Whether charging is available when the Location is closed.</summary>
    public bool? ChargingWhenClosed { get; init; }

    /// <summary>Images of the Location.</summary>
    public IReadOnlyList<Image>? Images { get; init; }

    /// <summary>Energy mix of the Location.</summary>
    public EnergyMix? EnergyMix { get; init; }

    /// <summary>Timestamp when this Location was last updated (UTC).</summary>
    [Required]
    public required DateTimeOffset LastUpdated { get; init; }
}
