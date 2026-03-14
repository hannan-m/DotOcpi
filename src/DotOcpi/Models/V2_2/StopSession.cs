using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// Command to stop a charging session.
/// </summary>
public sealed record StopSession
{
    /// <summary>HTTPS URL for the async result callback.</summary>
    [Required]
    [StringLength(255)]
    public required string ResponseUrl { get; init; }

    /// <summary>Session ID to stop.</summary>
    [Required]
    [StringLength(36)]
    public required CiString SessionId { get; init; }
}
