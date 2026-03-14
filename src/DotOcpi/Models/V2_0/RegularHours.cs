using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_0;

/// <summary>
/// Regular recurring operation hours. Weekday 1 = Monday, 7 = Sunday.
/// </summary>
public sealed record RegularHours
{
    /// <summary>Day of the week (1 = Monday, 7 = Sunday).</summary>
    [Required]
    [Range(1, 7)]
    public required int Weekday { get; init; }

    /// <summary>Begin of the regular period (HH:MM, 24h, local time).</summary>
    [Required]
    [StringLength(5)]
    public required string PeriodBegin { get; init; }

    /// <summary>End of the regular period (HH:MM, 24h, local time).</summary>
    [Required]
    [StringLength(5)]
    public required string PeriodEnd { get; init; }
}
