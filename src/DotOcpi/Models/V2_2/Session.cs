using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// A charging session.
/// </summary>
public sealed record Session
{
    /// <summary>ISO 3166-1 alpha-2 country code of the CPO.</summary>
    [Required]
    [StringLength(2)]
    public required CiString CountryCode { get; init; }

    /// <summary>CPO ID.</summary>
    [Required]
    [StringLength(3)]
    public required CiString PartyId { get; init; }

    /// <summary>Unique session identifier within the CPO's platform.</summary>
    [Required]
    [StringLength(36)]
    public required CiString Id { get; init; }

    /// <summary>Timestamp when the session started (UTC).</summary>
    [Required]
    public required DateTimeOffset StartDateTime { get; init; }

    /// <summary>Timestamp when the session ended (UTC).</summary>
    public DateTimeOffset? EndDateTime { get; init; }

    /// <summary>Energy delivered in kWh.</summary>
    [Required]
    public required decimal Kwh { get; init; }

    /// <summary>Token used for this session.</summary>
    [Required]
    public required CdrToken CdrToken { get; init; }

    /// <summary>Method used to authenticate the session.</summary>
    [Required]
    public required AuthMethod AuthMethod { get; init; }

    /// <summary>Reference from the authorization response.</summary>
    [StringLength(36)]
    public CiString? AuthorizationReference { get; init; }

    /// <summary>Location ID where the session takes place.</summary>
    [Required]
    [StringLength(36)]
    public required CiString LocationId { get; init; }

    /// <summary>EVSE UID used for this session.</summary>
    [Required]
    [StringLength(36)]
    public required CiString EvseUid { get; init; }

    /// <summary>Connector ID used for this session.</summary>
    [Required]
    [StringLength(36)]
    public required CiString ConnectorId { get; init; }

    /// <summary>Meter ID if available.</summary>
    [StringLength(255)]
    public string? MeterId { get; init; }

    /// <summary>ISO 4217 currency code.</summary>
    [Required]
    [StringLength(3)]
    public required string Currency { get; init; }

    /// <summary>Charging periods so far.</summary>
    public IReadOnlyList<ChargingPeriod>? ChargingPeriods { get; init; }

    /// <summary>Total cost so far. Changed from number to Price object in 2.2+.</summary>
    public Price? TotalCost { get; init; }

    /// <summary>Current status of the session.</summary>
    [Required]
    public required SessionStatus Status { get; init; }

    /// <summary>Timestamp when this session was last updated (UTC).</summary>
    [Required]
    public required DateTimeOffset LastUpdated { get; init; }
}
