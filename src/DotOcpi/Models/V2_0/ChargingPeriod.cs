using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_0;

/// <summary>
/// A period of charging with one or more dimensions. Used in both Sessions and CDRs.
/// </summary>
public sealed record ChargingPeriod
{
    /// <summary>Start timestamp of this charging period (UTC).</summary>
    [Required]
    public required DateTimeOffset StartDateTime { get; init; }

    /// <summary>Dimensions of this charging period. At least one required.</summary>
    [Required]
    public required IReadOnlyList<CdrDimension> Dimensions { get; init; }
}
