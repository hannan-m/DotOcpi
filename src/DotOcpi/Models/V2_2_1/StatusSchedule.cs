using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2_1;

/// <summary>
/// Planned future status of an EVSE.
/// </summary>
public sealed record StatusSchedule
{
    /// <summary>Start of the scheduled period (UTC).</summary>
    [Required]
    public required DateTimeOffset PeriodBegin { get; init; }

    /// <summary>End of the scheduled period (UTC). Open-ended if null.</summary>
    public DateTimeOffset? PeriodEnd { get; init; }

    /// <summary>Status during this period.</summary>
    [Required]
    public required Status Status { get; init; }
}
