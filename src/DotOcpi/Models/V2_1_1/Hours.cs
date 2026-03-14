namespace DotOcpi.Models.V2_1_1;

/// <summary>
/// Opening and access hours. Either <see cref="TwentyFourSeven"/> is true,
/// or <see cref="RegularHours"/> is provided (not both).
/// </summary>
public sealed record Hours
{
    /// <summary>True if available 24/7 (no regular hours needed).</summary>
    public bool? TwentyFourSeven { get; init; }

    /// <summary>Regular recurring hours (required when not 24/7).</summary>
    public IReadOnlyList<RegularHours>? RegularHours { get; init; }

    /// <summary>Exceptional periods where the location is open.</summary>
    public IReadOnlyList<ExceptionalPeriod>? ExceptionalOpenings { get; init; }

    /// <summary>Exceptional periods where the location is closed.</summary>
    public IReadOnlyList<ExceptionalPeriod>? ExceptionalClosings { get; init; }
}
