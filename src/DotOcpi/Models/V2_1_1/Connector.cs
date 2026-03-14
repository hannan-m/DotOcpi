using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_1_1;

/// <summary>
/// A connector is the socket or cable available for the EV to use.
/// In 2.1.1 uses voltage/amperage (not max_voltage/max_amperage), a single tariff_id,
/// and plain string IDs (not CiString).
/// </summary>
public sealed record Connector
{
    /// <summary>Unique identifier of the Connector within the EVSE.</summary>
    [Required]
    [StringLength(36)]
    public required string Id { get; init; }

    /// <summary>The standard of the installed connector.</summary>
    [Required]
    public required ConnectorType Standard { get; init; }

    /// <summary>The format (socket/cable) of the connector.</summary>
    [Required]
    public required ConnectorFormat Format { get; init; }

    /// <summary>The type of power at the connector.</summary>
    [Required]
    public required PowerType PowerType { get; init; }

    /// <summary>Voltage of the connector (line to neutral for AC_3_PHASE).</summary>
    [Required]
    public required int Voltage { get; init; }

    /// <summary>Amperage of the connector.</summary>
    [Required]
    public required int Amperage { get; init; }

    /// <summary>Identifier of the applicable charging tariff.</summary>
    [StringLength(36)]
    public string? TariffId { get; init; }

    /// <summary>URL to the operator's terms and conditions.</summary>
    [StringLength(255)]
    public string? TermsAndConditions { get; init; }

    /// <summary>Timestamp when this Connector was last updated (UTC).</summary>
    [Required]
    public required DateTimeOffset LastUpdated { get; init; }
}
