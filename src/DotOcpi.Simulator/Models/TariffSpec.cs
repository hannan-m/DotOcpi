namespace DotOcpi.Simulator.Models;

/// <summary>
/// Version-neutral tariff specification.
/// </summary>
public sealed record TariffSpec
{
    /// <summary>Tariff identifier.</summary>
    public required string Id { get; init; }

    /// <summary>ISO 4217 currency code.</summary>
    public string Currency { get; init; } = "EUR";

    /// <summary>Price per kWh excluding VAT.</summary>
    public decimal PricePerKwh { get; init; } = 0.39m;

    /// <summary>VAT rate as a decimal (e.g., 0.19 for 19%).</summary>
    public decimal VatRate { get; init; } = 0.19m;

    /// <summary>Pre-configured standard energy tariff.</summary>
    public static TariffSpec Default => new() { Id = "TAR1" };
}
