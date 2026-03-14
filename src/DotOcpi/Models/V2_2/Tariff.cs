using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// A charging tariff structure.
/// </summary>
public sealed record Tariff
{
    /// <summary>ISO 3166-1 alpha-2 country code of the CPO.</summary>
    [Required]
    [StringLength(2)]
    public required CiString CountryCode { get; init; }

    /// <summary>CPO ID.</summary>
    [Required]
    [StringLength(3)]
    public required CiString PartyId { get; init; }

    /// <summary>Unique tariff identifier within the CPO's platform.</summary>
    [Required]
    [StringLength(36)]
    public required CiString Id { get; init; }

    /// <summary>ISO 4217 currency code.</summary>
    [Required]
    [StringLength(3)]
    public required string Currency { get; init; }

    /// <summary>Type of tariff (2.2+ only).</summary>
    public TariffType? Type { get; init; }

    /// <summary>Alternative human-readable tariff descriptions.</summary>
    public IReadOnlyList<DisplayText>? TariffAltText { get; init; }

    /// <summary>URL to a web page with tariff details.</summary>
    [StringLength(255)]
    public string? TariffAltUrl { get; init; }

    /// <summary>Minimum price to be billed (2.2+ only).</summary>
    public Price? MinPrice { get; init; }

    /// <summary>Maximum price to be billed (2.2+ only).</summary>
    public Price? MaxPrice { get; init; }

    /// <summary>Tariff elements. At least one required.</summary>
    [Required]
    public required IReadOnlyList<TariffElement> Elements { get; init; }

    /// <summary>When the tariff becomes active (2.2+ only).</summary>
    public DateTimeOffset? StartDateTime { get; init; }

    /// <summary>When the tariff expires (2.2+ only).</summary>
    public DateTimeOffset? EndDateTime { get; init; }

    /// <summary>Energy mix for this tariff.</summary>
    public EnergyMix? EnergyMix { get; init; }

    /// <summary>Timestamp when this tariff was last updated (UTC).</summary>
    [Required]
    public required DateTimeOffset LastUpdated { get; init; }
}
