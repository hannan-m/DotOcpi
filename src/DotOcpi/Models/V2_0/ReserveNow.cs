using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_0;

/// <summary>
/// Command to reserve a charging location.
/// In 2.0: reservation_id is int (not string), no authorization_reference.
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

    /// <summary>Reservation identifier (int in 2.0, changed to string in 2.2+).</summary>
    [Required]
    public required int ReservationId { get; init; }

    /// <summary>Location to reserve.</summary>
    [Required]
    [StringLength(36)]
    public required string LocationId { get; init; }

    /// <summary>Specific EVSE to reserve (optional).</summary>
    [StringLength(36)]
    public string? EvseUid { get; init; }
}
