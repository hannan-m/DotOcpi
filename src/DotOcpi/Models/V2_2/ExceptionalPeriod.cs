using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// A period of exceptional opening or closing hours.
/// </summary>
public sealed record ExceptionalPeriod
{
    /// <summary>Start of the exceptional period (UTC).</summary>
    [Required]
    public required DateTimeOffset PeriodBegin { get; init; }

    /// <summary>End of the exceptional period (UTC).</summary>
    [Required]
    public required DateTimeOffset PeriodEnd { get; init; }
}
