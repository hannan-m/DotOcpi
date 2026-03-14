using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_1_1;

/// <summary>
/// A charging tariff structure.
/// In 2.1.1: no country_code/party_id, no type, no min_price/max_price,
/// no start_date_time/end_date_time.
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

    /// <summary>Energy mix for this tariff.</summary>
    public EnergyMix? EnergyMix { get; init; }

    /// <summary>Timestamp when this tariff was last updated (UTC).</summary>
    [Required]
    public required DateTimeOffset LastUpdated { get; init; }
}
