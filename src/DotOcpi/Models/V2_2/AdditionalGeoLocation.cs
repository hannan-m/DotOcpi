using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// An additional geo location related to a Location, e.g. entrance coordinates.
/// </summary>
public sealed record AdditionalGeoLocation
{
    /// <summary>Latitude (decimal, e.g. "50.770774").</summary>
    [Required]
    [StringLength(10)]
    public required string Latitude { get; init; }

    /// <summary>Longitude (decimal, e.g. "-126.104965").</summary>
    [Required]
    [StringLength(11)]
    public required string Longitude { get; init; }

    /// <summary>Optional display name for this location.</summary>
    public DisplayText? Name { get; init; }
}
