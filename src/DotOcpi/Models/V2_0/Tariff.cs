using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_0;

/// <summary>
/// A charging tariff structure.
/// In 2.0: no last_updated timestamp and no energy_mix field.
/// </summary>
public sealed record Tariff
{
    /// <summary>Unique tariff identifier within the CPO's platform.</summary>
    [Required]
    [StringLength(36)]
    public required string Id { get; init; }

    /// <summary>ISO 4217 currency code.</summary>
    [Required]
    [StringLength(3)]
    public required string Currency { get; init; }

    /// <summary>Alternative human-readable tariff descriptions.</summary>
    public IReadOnlyList<DisplayText>? TariffAltText { get; init; }

    /// <summary>URL to a web page with tariff details.</summary>
    [StringLength(255)]
    public string? TariffAltUrl { get; init; }

    /// <summary>Tariff elements. At least one required.</summary>
    [Required]
    public required IReadOnlyList<TariffElement> Elements { get; init; }
}
