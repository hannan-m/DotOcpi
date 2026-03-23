namespace DotOcpi.Simulator.State;

/// <summary>
/// Active EVSE reservation with expiry tracking.
/// </summary>
internal sealed class ReservationState
{
    public required string ReservationId { get; init; }
    public required string LocationId { get; init; }
    public required string EvseUid { get; init; }
    public required string TokenUid { get; init; }
    public required DateTimeOffset ExpiryDate { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
