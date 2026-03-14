using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2_1;

/// <summary>
/// Asynchronous result of a set-charging-profile request.
/// </summary>
public sealed record ChargingProfileResult
{
    /// <summary>Result of the charging profile request.</summary>
    [Required]
    public required ChargingProfileResultType Result { get; init; }
}
