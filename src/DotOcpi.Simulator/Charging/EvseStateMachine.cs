using DotOcpi.Simulator.State;

namespace DotOcpi.Simulator.Charging;

/// <summary>
/// Pure function that enforces valid EVSE status transitions.
/// Models real CPO behavior: AVAILABLE → RESERVED → CHARGING → AVAILABLE.
/// </summary>
internal static class EvseStateMachine
{
    /// <summary>
    /// Attempts a status transition. Returns the new status and whether the transition was allowed.
    /// </summary>
    public static (EvseStatus NewStatus, bool Allowed) Transition(EvseStatus current, EvseEvent trigger)
    {
        return (current, trigger) switch
        {
            (EvseStatus.Available, EvseEvent.ReserveNow) => (EvseStatus.Reserved, true),
            (EvseStatus.Available, EvseEvent.StartSession) => (EvseStatus.Charging, true),
            (EvseStatus.Reserved, EvseEvent.StartSession) => (EvseStatus.Charging, true),
            (EvseStatus.Reserved, EvseEvent.CancelReservation) => (EvseStatus.Available, true),
            (EvseStatus.Reserved, EvseEvent.ReservationExpired) => (EvseStatus.Available, true),
            (EvseStatus.Charging, EvseEvent.StopSession) => (EvseStatus.Available, true),
            (EvseStatus.Charging, EvseEvent.FaultDetected) => (EvseStatus.Inoperative, true),
            (_, EvseEvent.FaultDetected) => (EvseStatus.Inoperative, true),
            (_, EvseEvent.FaultCleared) when current is EvseStatus.Inoperative or EvseStatus.OutOfOrder => (
                EvseStatus.Available,
                true
            ),
            _ => (current, false),
        };
    }
}

/// <summary>
/// Events that trigger EVSE status transitions.
/// </summary>
internal enum EvseEvent
{
    ReserveNow,
    CancelReservation,
    ReservationExpired,
    StartSession,
    StopSession,
    FaultDetected,
    FaultCleared,
}
