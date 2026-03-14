using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_0;

/// <summary>
/// A Charge Detail Record — the final billing record for a completed session.
/// In 2.0: no last_updated timestamp.
/// </summary>
public sealed record Cdr
{
    /// <summary>Unique CDR identifier within the CPO's platform.</summary>
    [Required]
    [StringLength(39)]
    public required string Id { get; init; }

    /// <summary>Start timestamp of the charging session (UTC).</summary>
    [Required]
    public required DateTimeOffset StartDateTime { get; init; }

    /// <summary>End timestamp of the charging session (UTC).</summary>
    [Required]
    public required DateTimeOffset EndDateTime { get; init; }

    /// <summary>Reference to the authorization used (token auth_id).</summary>
    [Required]
    [StringLength(36)]
    public required string AuthId { get; init; }

    /// <summary>Authentication method used.</summary>
    [Required]
    public required AuthMethod AuthMethod { get; init; }

    /// <summary>Location where the session took place.</summary>
    [Required]
    public required Location Location { get; init; }

    /// <summary>Meter ID if available.</summary>
    [StringLength(255)]
    public string? MeterId { get; init; }

    /// <summary>ISO 4217 currency code.</summary>
    [Required]
    [StringLength(3)]
    public required string Currency { get; init; }

    /// <summary>Tariffs applicable to this CDR.</summary>
    public IReadOnlyList<Tariff>? Tariffs { get; init; }

    /// <summary>Charging periods. At least one required.</summary>
    [Required]
    public required IReadOnlyList<ChargingPeriod> ChargingPeriods { get; init; }

    /// <summary>Total cost of this CDR (plain decimal in 2.0).</summary>
    [Required]
    public required decimal TotalCost { get; init; }

    /// <summary>Total energy delivered in kWh.</summary>
    [Required]
    public required decimal TotalEnergy { get; init; }

    /// <summary>Total time of the session in hours.</summary>
    [Required]
    public required decimal TotalTime { get; init; }

    /// <summary>Total parking time in hours.</summary>
    public decimal? TotalParkingTime { get; init; }

    /// <summary>Optional remark from the CPO.</summary>
    [StringLength(255)]
    public string? Remark { get; init; }
}
