using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// Command to unlock a connector at a Location.
/// </summary>
public sealed record UnlockConnector
{
    /// <summary>HTTPS URL for the async result callback.</summary>
    [Required]
    [StringLength(255)]
    public required string ResponseUrl { get; init; }

    /// <summary>Location ID.</summary>
    [Required]
    [StringLength(36)]
    public required CiString LocationId { get; init; }

    /// <summary>EVSE UID.</summary>
    [Required]
    [StringLength(36)]
    public required CiString EvseUid { get; init; }

    /// <summary>Connector ID to unlock.</summary>
    [Required]
    [StringLength(36)]
    public required CiString ConnectorId { get; init; }
}
