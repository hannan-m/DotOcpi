using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// Asynchronous result of a get-active-charging-profile request.
/// </summary>
public sealed record ActiveChargingProfileResult
{
    /// <summary>Result of the request.</summary>
    [Required]
    public required ChargingProfileResultType Result { get; init; }

    /// <summary>The active profile, if result is ACCEPTED.</summary>
    public ActiveChargingProfile? Profile { get; init; }
}
