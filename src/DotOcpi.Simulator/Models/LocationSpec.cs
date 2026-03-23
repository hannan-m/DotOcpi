namespace DotOcpi.Simulator.Models;

/// <summary>
/// Version-neutral location specification. Configure once, the
/// <see cref="VersionModelBuilder"/> maps to the correct version-specific model.
/// </summary>
public sealed record LocationSpec
{
    /// <summary>Location identifier (e.g., "LOC1").</summary>
    public required string Id { get; init; }

    /// <summary>Display name of the location.</summary>
    public string Name { get; init; } = "Charging Station";

    /// <summary>Street address.</summary>
    public string Address { get; init; } = "Hauptstraße 1";

    /// <summary>City or town.</summary>
    public string City { get; init; } = "Berlin";

    /// <summary>Postal code.</summary>
    public string PostalCode { get; init; } = "10115";

    /// <summary>ISO 3166-1 alpha-3 country code.</summary>
    public string Country { get; init; } = "DEU";

    /// <summary>Latitude as string (preserves wire precision).</summary>
    public string Latitude { get; init; } = "52.520008";

    /// <summary>Longitude as string (preserves wire precision).</summary>
    public string Longitude { get; init; } = "13.404954";

    /// <summary>IANA time zone (required in OCPI 2.2+).</summary>
    public string TimeZone { get; init; } = "Europe/Berlin";

    /// <summary>EVSEs at this location. Defaults to one DC fast charger.</summary>
    public IReadOnlyList<EvseSpec> Evses { get; init; } = [EvseSpec.Default];

    /// <summary>Pre-configured Berlin DC fast charging station.</summary>
    public static LocationSpec Default => new() { Id = "LOC1" };
}

/// <summary>
/// Version-neutral EVSE specification.
/// </summary>
public sealed record EvseSpec
{
    /// <summary>Internal EVSE UID.</summary>
    public required string Uid { get; init; }

    /// <summary>DIN/ISO standard EVSE ID (optional).</summary>
    public string? EvseId { get; init; }

    /// <summary>Connector at this EVSE.</summary>
    public ConnectorProfile Connector { get; init; } = ConnectorProfile.DcFast;

    /// <summary>EVSE capabilities.</summary>
    public IReadOnlyList<string> Capabilities { get; init; } =
    ["REMOTE_START_STOP_CAPABLE", "RESERVABLE", "RFID_READER"];

    /// <summary>Pre-configured DC fast EVSE.</summary>
    public static EvseSpec Default => new() { Uid = "EVSE001", EvseId = "DE*CPO*E001" };
}
