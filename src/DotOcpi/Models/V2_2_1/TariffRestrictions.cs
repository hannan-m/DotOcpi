namespace DotOcpi.Models.V2_2_1;

/// <summary>
/// Restrictions that apply to a tariff element.
/// </summary>
public sealed record TariffRestrictions
{
    /// <summary>Start time of day (HH:MM, local time).</summary>
    public string? StartTime { get; init; }

    /// <summary>End time of day (HH:MM, local time).</summary>
    public string? EndTime { get; init; }

    /// <summary>Start date (YYYY-MM-DD, local time).</summary>
    public string? StartDate { get; init; }

    /// <summary>End date (YYYY-MM-DD, local time).</summary>
    public string? EndDate { get; init; }

    /// <summary>Minimum consumed energy in kWh.</summary>
    public decimal? MinKwh { get; init; }

    /// <summary>Maximum consumed energy in kWh.</summary>
    public decimal? MaxKwh { get; init; }

    /// <summary>Minimum current in A (2.2+ only).</summary>
    public decimal? MinCurrent { get; init; }

    /// <summary>Maximum current in A (2.2+ only).</summary>
    public decimal? MaxCurrent { get; init; }

    /// <summary>Minimum power in kW.</summary>
    public decimal? MinPower { get; init; }

    /// <summary>Maximum power in kW.</summary>
    public decimal? MaxPower { get; init; }

    /// <summary>Minimum duration in seconds.</summary>
    public int? MinDuration { get; init; }

    /// <summary>Maximum duration in seconds.</summary>
    public int? MaxDuration { get; init; }

    /// <summary>Which days of the week this restriction applies to.</summary>
    public IReadOnlyList<OcpiDayOfWeek>? DayOfWeek { get; init; }

    /// <summary>Reservation restriction type (2.2+ only).</summary>
    public ReservationRestrictionType? Reservation { get; init; }
}
