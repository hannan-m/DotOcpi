namespace DotOcpi.Simulator.Models;

/// <summary>
/// Version-neutral connector hardware specification.
/// Holds physical characteristics used by the charging simulation
/// and mapped to version-specific models at serialization time.
/// </summary>
public sealed record ConnectorProfile
{
    /// <summary>Connector ID within the EVSE (e.g., "1").</summary>
    public required string Id { get; init; }

    /// <summary>Connector standard (e.g., "IEC_62196_T2_COMBO", "CHADEMO").</summary>
    public string Standard { get; init; } = "IEC_62196_T2_COMBO";

    /// <summary>Connector format: "SOCKET" or "CABLE".</summary>
    public string Format { get; init; } = "CABLE";

    /// <summary>Power type: "AC_1_PHASE", "AC_3_PHASE", or "DC".</summary>
    public string PowerType { get; init; } = "DC";

    /// <summary>Maximum voltage in volts.</summary>
    public int MaxVoltage { get; init; } = 400;

    /// <summary>Maximum amperage in amps.</summary>
    public int MaxAmperage { get; init; } = 125;

    /// <summary>Maximum power in kW (derived from voltage/amperage if not set).</summary>
    public decimal MaxPowerKw { get; init; } = 50m;

    /// <summary>Optional tariff ID applied to this connector.</summary>
    public string? TariffId { get; init; }

    /// <summary>Pre-configured DC fast charging connector (CCS, 50 kW).</summary>
    public static ConnectorProfile DcFast =>
        new()
        {
            Id = "1",
            Standard = "IEC_62196_T2_COMBO",
            Format = "CABLE",
            PowerType = "DC",
            MaxVoltage = 400,
            MaxAmperage = 125,
            MaxPowerKw = 50m,
        };

    /// <summary>Pre-configured DC ultra-fast connector (CCS, 150 kW).</summary>
    public static ConnectorProfile DcUltraFast =>
        new()
        {
            Id = "1",
            Standard = "IEC_62196_T2_COMBO",
            Format = "CABLE",
            PowerType = "DC",
            MaxVoltage = 800,
            MaxAmperage = 200,
            MaxPowerKw = 150m,
        };

    /// <summary>Pre-configured AC Type 2 connector (11 kW).</summary>
    public static ConnectorProfile AcStandard =>
        new()
        {
            Id = "1",
            Standard = "IEC_62196_T2",
            Format = "SOCKET",
            PowerType = "AC_3_PHASE",
            MaxVoltage = 230,
            MaxAmperage = 16,
            MaxPowerKw = 11m,
        };

    /// <summary>Pre-configured AC Type 2 connector (22 kW).</summary>
    public static ConnectorProfile AcFast =>
        new()
        {
            Id = "1",
            Standard = "IEC_62196_T2",
            Format = "SOCKET",
            PowerType = "AC_3_PHASE",
            MaxVoltage = 230,
            MaxAmperage = 32,
            MaxPowerKw = 22m,
        };
}
