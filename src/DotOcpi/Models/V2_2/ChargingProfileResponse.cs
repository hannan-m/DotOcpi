using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// Synchronous response to a charging profile request.
/// </summary>
public sealed record ChargingProfileResponse
{
    /// <summary>Whether the request was accepted.</summary>
    [Required]
    public required ChargingProfileResponseType Result { get; init; }

    /// <summary>Timeout in seconds to wait for the async result.</summary>
    [Required]
    public required int Timeout { get; init; }
}
