using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2_1;

/// <summary>
/// Command to reserve a charging location.
/// </summary>
public sealed record ReserveNow
{
    /// <summary>HTTPS URL for the async result callback.</summary>
    [Required]
    [StringLength(255)]
    public required string ResponseUrl { get; init; }

    /// <summary>Token to use for the reservation.</summary>
    [Required]
    public required Token Token { get; init; }

    /// <summary>When the reservation expires (must be in the future).</summary>
    [Required]
    public required DateTimeOffset ExpiryDate { get; init; }

    /// <summary>Reservation identifier (changed from int to string in 2.2+).</summary>
    [Required]
    [StringLength(36)]
    public required CiString ReservationId { get; init; }

    /// <summary>Location to reserve.</summary>
    [Required]
    [StringLength(36)]
    public required CiString LocationId { get; init; }

    /// <summary>Specific EVSE to reserve (optional).</summary>
    [StringLength(36)]
    public CiString? EvseUid { get; init; }

    /// <summary>Reference from prior authorization (2.2+ only).</summary>
    [StringLength(36)]
    public CiString? AuthorizationReference { get; init; }
}
