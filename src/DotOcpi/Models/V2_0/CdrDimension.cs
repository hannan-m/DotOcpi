using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_0;

/// <summary>
/// A dimension of a charging period (energy, time, etc.) with its volume.
/// </summary>
public sealed record CdrDimension
{
    /// <summary>The type of this dimension.</summary>
    [Required]
    public required CdrDimensionType Type { get; init; }

    /// <summary>Volume of this dimension consumed during the charging period.</summary>
    [Required]
    public required decimal Volume { get; init; }
}
