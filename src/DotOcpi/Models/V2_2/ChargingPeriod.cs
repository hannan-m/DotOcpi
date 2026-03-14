using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2;

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

    /// <summary>Tariff that applies to this period (2.2+ only).</summary>
    [StringLength(36)]
    public CiString? TariffId { get; init; }
}
