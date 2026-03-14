using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// A price with optional VAT. Used for total costs in Sessions and CDRs.
/// </summary>
public sealed record Price
{
    /// <summary>Price excluding VAT.</summary>
    [Required]
    public required decimal ExclVat { get; init; }

    /// <summary>Price including VAT.</summary>
    public decimal? InclVat { get; init; }
}
