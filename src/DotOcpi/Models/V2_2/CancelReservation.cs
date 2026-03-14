using System.ComponentModel.DataAnnotations;

namespace DotOcpi.Models.V2_2;

/// <summary>
/// Command to cancel a reservation (2.2+ only).
/// </summary>
public sealed record CancelReservation
{
    /// <summary>HTTPS URL for the async result callback.</summary>
    [Required]
    [StringLength(255)]
    public required string ResponseUrl { get; init; }

    /// <summary>Reservation ID to cancel.</summary>
    [Required]
    [StringLength(36)]
    public required CiString ReservationId { get; init; }
}
