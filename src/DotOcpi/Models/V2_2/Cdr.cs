using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// A Charge Detail Record — the final billing record for a completed session.
/// </summary>
public sealed record Cdr
{
    /// <summary>ISO 3166-1 alpha-2 country code of the CPO.</summary>
    [Required]
    [StringLength(2)]
    public required CiString CountryCode { get; init; }

    /// <summary>CPO ID.</summary>
    [Required]
    [StringLength(3)]
    public required CiString PartyId { get; init; }

    /// <summary>Unique CDR identifier within the CPO's platform.</summary>
    [Required]
    [StringLength(39)]
    public required CiString Id { get; init; }

    /// <summary>Start timestamp of the charging session (UTC).</summary>
    [Required]
    public required DateTimeOffset StartDateTime { get; init; }

    /// <summary>End timestamp of the charging session (UTC).</summary>
    [Required]
    public required DateTimeOffset EndDateTime { get; init; }

    /// <summary>Reference to the Session, if available.</summary>
    [StringLength(36)]
    public CiString? SessionId { get; init; }

    /// <summary>Token used for this CDR.</summary>
    [Required]
    public required CdrToken CdrToken { get; init; }

    /// <summary>Authentication method used.</summary>
    [Required]
    public required AuthMethod AuthMethod { get; init; }

    /// <summary>Reference from the authorization response.</summary>
    [StringLength(36)]
    public CiString? AuthorizationReference { get; init; }

    /// <summary>Location snapshot at the time of the CDR.</summary>
    [Required]
    public required CdrLocation CdrLocation { get; init; }

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

    /// <summary>Signed metering data for calibration law compliance.</summary>
    public SignedData? SignedData { get; init; }

    /// <summary>Total cost of this CDR.</summary>
    [Required]
    public required Price TotalCost { get; init; }

    /// <summary>Total fixed cost.</summary>
    public Price? TotalFixedCost { get; init; }

    /// <summary>Total energy delivered in kWh.</summary>
    [Required]
    public required decimal TotalEnergy { get; init; }

    /// <summary>Total energy cost.</summary>
    public Price? TotalEnergyCost { get; init; }

    /// <summary>Total time of the session in hours.</summary>
    [Required]
    public required decimal TotalTime { get; init; }

    /// <summary>Total time cost.</summary>
    public Price? TotalTimeCost { get; init; }

    /// <summary>Total parking time in hours.</summary>
    public decimal? TotalParkingTime { get; init; }

    /// <summary>Total parking cost.</summary>
    public Price? TotalParkingCost { get; init; }

    /// <summary>Total reservation cost.</summary>
    public Price? TotalReservationCost { get; init; }

    /// <summary>Optional remark from the CPO.</summary>
    [StringLength(255)]
    public string? Remark { get; init; }

    /// <summary>Invoice reference ID.</summary>
    [StringLength(39)]
    public CiString? InvoiceReferenceId { get; init; }

    /// <summary>Whether this is a credit CDR (reversal).</summary>
    public bool? Credit { get; init; }

    /// <summary>Reference to the CDR that is being credited.</summary>
    [StringLength(39)]
    public CiString? CreditReferenceId { get; init; }

    /// <summary>Timestamp when this CDR was last updated (UTC).</summary>
    [Required]
    public required DateTimeOffset LastUpdated { get; init; }
}
