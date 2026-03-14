using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_1_1;

/// <summary>
/// An element of a tariff with price components and optional restrictions.
/// </summary>
public sealed record TariffElement
{
    /// <summary>Price components of this element. At least one required.</summary>
    [Required]
    public required IReadOnlyList<PriceComponent> PriceComponents { get; init; }

    /// <summary>Restrictions that apply to this element.</summary>
    public TariffRestrictions? Restrictions { get; init; }
}
