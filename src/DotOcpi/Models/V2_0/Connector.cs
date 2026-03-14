using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_0;

/// <summary>
/// A connector is the socket or cable available for the EV to use.
/// In 2.0: no tariff_id field and no last_updated timestamp.
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

    /// <summary>URL to the operator's terms and conditions.</summary>
    [StringLength(255)]
    public string? TermsAndConditions { get; init; }
}
