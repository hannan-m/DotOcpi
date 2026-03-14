using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_1_1;

/// <summary>
/// A price component of a tariff element.
/// In 2.1.1 there is no vat field on PriceComponent.
/// </summary>
public sealed record PriceComponent
{
    /// <summary>Type of tariff dimension.</summary>
    [Required]
    public required TariffDimensionType Type { get; init; }

    /// <summary>Price per unit for this component.</summary>
    [Required]
    public required decimal Price { get; init; }

    /// <summary>Minimum amount to be billed. Must be &gt; 0.</summary>
    [Required]
    [Range(1, int.MaxValue)]
    public required int StepSize { get; init; }
}
