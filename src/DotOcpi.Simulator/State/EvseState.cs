using DotOcpi.Simulator.Models;

namespace DotOcpi.Simulator.State;

/// <summary>
/// Mutable EVSE status tracking. Stores current status and links
/// to active session/reservation for state machine transitions.
/// </summary>
internal sealed class EvseState
{
    private readonly object _lock = new();

    public required string LocationId { get; init; }
    public required string EvseUid { get; init; }
    public required ConnectorProfile Connector { get; init; }

    public EvseStatus Status { get; set; } = EvseStatus.Available;
    public string? ActiveSessionId { get; set; }
    public string? ActiveReservationId { get; set; }
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;

    public string Key => $"{LocationId}:{EvseUid}";

    /// <summary>Atomically reads all mutable fields under one lock.</summary>
    public (
        EvseStatus Status,
        string? ActiveSessionId,
        string? ActiveReservationId,
        DateTimeOffset LastUpdated
    ) Snapshot()
    {
        lock (_lock)
            return (Status, ActiveSessionId, ActiveReservationId, LastUpdated);
    }

    /// <summary>
    /// Atomically updates status and optionally sets session/reservation IDs.
    /// Pass a value to set it, or omit to leave unchanged.
    /// </summary>
    public void Update(
        EvseStatus status,
        string? activeSessionId = null,
        string? activeReservationId = null,
        bool clearSession = false,
        bool clearReservation = false
    )
    {
        lock (_lock)
        {
            Status = status;
            if (clearSession)
                ActiveSessionId = null;
            else if (activeSessionId is not null)
                ActiveSessionId = activeSessionId;
            if (clearReservation)
                ActiveReservationId = null;
            else if (activeReservationId is not null)
                ActiveReservationId = activeReservationId;
            LastUpdated = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>Clears the active session under lock.</summary>
    public void ClearSession(EvseStatus newStatus)
    {
        lock (_lock)
        {
            Status = newStatus;
            ActiveSessionId = null;
            LastUpdated = DateTimeOffset.UtcNow;
        }
    }
}

/// <summary>
/// Version-neutral EVSE status. Maps to the per-version Status enum at serialization time.
/// </summary>
public enum EvseStatus
{
    Available,
    Reserved,
    Charging,
    Inoperative,
    OutOfOrder,
    Blocked,
    Planned,
    Removed,
    Unknown,
}
