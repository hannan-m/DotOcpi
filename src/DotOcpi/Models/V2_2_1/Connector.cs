using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2_1;

/// <summary>
/// A connector is the socket or cable available for the EV to use.
/// </summary>
public sealed record Connector
{
    /// <summary>Unique identifier of the Connector within the EVSE.</summary>
    [Required]
    [StringLength(36)]
    public required CiString Id { get; init; }

    /// <summary>The standard of the installed connector.</summary>
    [Required]
    public required ConnectorType Standard { get; init; }

    /// <summary>The format (socket/cable) of the connector.</summary>
    [Required]
    public required ConnectorFormat Format { get; init; }

    /// <summary>The type of power at the connector.</summary>
    [Required]
    public required PowerType PowerType { get; init; }

    /// <summary>Maximum voltage of the connector (line to neutral for AC_3_PHASE).</summary>
    [Required]
    public required int MaxVoltage { get; init; }

    /// <summary>Maximum amperage of the connector.</summary>
    [Required]
    public required int MaxAmperage { get; init; }

    /// <summary>Maximum electric power that can be delivered (Watts). Derived from voltage/amperage if absent.</summary>
    public int? MaxElectricPower { get; init; }

    /// <summary>Identifiers of the valid charging tariffs. Multiple tariffs possible for different TokenTypes.</summary>
    public IReadOnlyList<CiString>? TariffIds { get; init; }

    /// <summary>URL to the operator's terms and conditions.</summary>
    [StringLength(255)]
    public string? TermsAndConditions { get; init; }

    /// <summary>Timestamp when this Connector was last updated (UTC).</summary>
    [Required]
    public required DateTimeOffset LastUpdated { get; init; }
}
