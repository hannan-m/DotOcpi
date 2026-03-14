using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_0;

/// <summary>
/// A charging session.
/// In 2.0: no last_updated timestamp and no total_cost field.
/// </summary>
public sealed record Session
{
    /// <summary>Unique session identifier within the CPO's platform.</summary>
    [Required]
    [StringLength(36)]
    public required string Id { get; init; }

    /// <summary>Timestamp when the session started (UTC).</summary>
    [Required]
    public required DateTimeOffset StartDateTime { get; init; }

    /// <summary>Timestamp when the session ended (UTC).</summary>
    public DateTimeOffset? EndDateTime { get; init; }

    /// <summary>Energy delivered in kWh.</summary>
    [Required]
    public required decimal Kwh { get; init; }

    /// <summary>Reference to the authorization used (token auth_id).</summary>
    [Required]
    [StringLength(36)]
    public required string AuthId { get; init; }

    /// <summary>Method used to authenticate the session.</summary>
    [Required]
    public required AuthMethod AuthMethod { get; init; }

    /// <summary>Location where the session takes place.</summary>
    [Required]
    public required Location Location { get; init; }

    /// <summary>Meter ID if available.</summary>
    [StringLength(255)]
    public string? MeterId { get; init; }

    /// <summary>ISO 4217 currency code.</summary>
    [Required]
    [StringLength(3)]
    public required string Currency { get; init; }

    /// <summary>Charging periods so far.</summary>
    public IReadOnlyList<ChargingPeriod>? ChargingPeriods { get; init; }

    /// <summary>Current status of the session.</summary>
    [Required]
    public required SessionStatus Status { get; init; }
}
