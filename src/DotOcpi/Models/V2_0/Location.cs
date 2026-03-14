using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_0;

/// <summary>
/// A charging location with one or more EVSEs.
/// In 2.0: no last_updated, no energy_mix, no facilities, no time_zone,
/// no location type (LocationType was added in 2.1.1), no charging_when_closed.
/// </summary>
public sealed record Location
{
    /// <summary>Unique identifier of the Location within the CPO's platform.</summary>
    [Required]
    [StringLength(39)]
    public required string Id { get; init; }

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

    /// <summary>Postal code. Required in 2.0.</summary>
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

    /// <summary>Hours of operation.</summary>
    public Hours? OpeningTimes { get; init; }

    /// <summary>Images of the Location.</summary>
    public IReadOnlyList<Image>? Images { get; init; }
}
