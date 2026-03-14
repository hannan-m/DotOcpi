using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_1_1;

/// <summary>
/// A period of charging with one or more dimensions. Used in both Sessions and CDRs.
/// In 2.1.1 there is no tariff_id on ChargingPeriod.
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
