namespace DotOcpi.Simulator.Charging;

/// <summary>
/// Defines charging behavior for a simulated session: power curve, cost, and target energy.
/// </summary>
public sealed record ChargingProfileSpec
{
    /// <summary>Maximum charging power in kW.</summary>
    public decimal MaxPowerKw { get; init; } = 50m;

    /// <summary>
    /// For DC charging: the SOC fraction (0.0-1.0) where power tapering begins.
    /// Set to 1.0 for flat power (AC or DC without tapering).
    /// </summary>
    public decimal TaperStartFraction { get; init; } = 0.8m;

    /// <summary>Final power in kW when near target (DC tapering only).</summary>
    public decimal TaperFinalPowerKw { get; init; } = 5m;

    /// <summary>Target energy capacity in kWh (simulates battery size).</summary>
    public decimal TargetKwh { get; init; } = 60m;

    /// <summary>Price per kWh excluding VAT.</summary>
    public decimal PricePerKwh { get; init; } = 0.39m;

    /// <summary>VAT rate as a decimal (e.g., 0.19 for 19%).</summary>
    public decimal VatRate { get; init; } = 0.19m;

    /// <summary>Whether this profile uses DC tapering (CC/CV curve).</summary>
    public bool IsDcTapering => TaperStartFraction < 1.0m;

    /// <summary>DC fast charging: 50 kW CCS with CC/CV tapering at 80% SOC.</summary>
    public static ChargingProfileSpec DcFast =>
        new()
        {
            MaxPowerKw = 50m,
            TaperStartFraction = 0.8m,
            TaperFinalPowerKw = 5m,
            TargetKwh = 60m,
        };

    /// <summary>DC ultra-fast: 150 kW CCS with tapering at 75% SOC.</summary>
    public static ChargingProfileSpec DcUltraFast =>
        new()
        {
            MaxPowerKw = 150m,
            TaperStartFraction = 0.75m,
            TaperFinalPowerKw = 10m,
            TargetKwh = 80m,
        };

    /// <summary>AC standard: 11 kW flat power, no tapering.</summary>
    public static ChargingProfileSpec AcStandard =>
        new()
        {
            MaxPowerKw = 11m,
            TaperStartFraction = 1.0m,
            TargetKwh = 40m,
        };

    /// <summary>AC fast: 22 kW flat power, no tapering.</summary>
    public static ChargingProfileSpec AcFast =>
        new()
        {
            MaxPowerKw = 22m,
            TaperStartFraction = 1.0m,
            TargetKwh = 60m,
        };
}
