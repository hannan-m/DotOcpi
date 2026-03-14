using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_1_1;

/// <summary>
/// A charging location with one or more EVSEs.
/// In 2.1.1: no country_code/party_id on the object, no publish/publish_allowed_to,
/// uses LocationType instead of ParkingType, postal_code is required, time_zone is optional.
/// </summary>
public sealed record Location
{
    /// <summary>Unique identifier of the Location within the CPO's platform.</summary>
    [Required]
    [StringLength(39)]
    public required string Id { get; init; }

    /// <summary>The general type of the charge point location.</summary>
    public LocationType? Type { get; init; }

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

    /// <summary>Postal code. Required in 2.1.1.</summary>
    [Required]
    [StringLength(10)]
    public required string PostalCode { get; init; }

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

    /// <summary>IANA time zone of the Location. Optional in 2.1.1.</summary>
    [StringLength(255)]
    public string? TimeZone { get; init; }

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
