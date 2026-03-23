namespace DotOcpi.Simulator.State;

/// <summary>
/// Mutable session with charging progress. Updated on each charging tick
/// and finalized when the session completes. All mutable field access
/// must go through the lock to prevent concurrent modification.
/// </summary>
public sealed class SessionState
{
    private readonly object _lock = new();

    public required string SessionId { get; init; }
    public required string LocationId { get; init; }
    public required string EvseUid { get; init; }
    public required string ConnectorId { get; init; }
    public required string TokenUid { get; init; }
    public required string TokenContractId { get; init; }
    public required DateTimeOffset StartTime { get; init; }
    public required string ConnectionId { get; init; }

    public DateTimeOffset? EndTime { get; set; }
    public decimal KwhDelivered { get; set; }
    public decimal TotalCostExclVat { get; set; }
    public decimal TotalCostInclVat { get; set; }
    public SessionPhase Phase { get; set; } = SessionPhase.Active;
    public List<ChargingPeriodState> ChargingPeriods { get; } = [];
    public DateTimeOffset LastUpdated { get; set; }

    /// <summary>Executes an action under the session lock.</summary>
    public void WithLock(Action<SessionState> action)
    {
        lock (_lock)
            action(this);
    }

    /// <summary>Executes a function under the session lock and returns the result.</summary>
    public T WithLock<T>(Func<SessionState, T> func)
    {
        lock (_lock)
            return func(this);
    }

    /// <summary>Takes a consistent snapshot of all mutable fields under one lock.</summary>
    public SessionSnapshot Snapshot()
    {
        lock (_lock)
        {
            return new SessionSnapshot
            {
                Phase = Phase,
                EndTime = EndTime,
                KwhDelivered = KwhDelivered,
                TotalCostExclVat = TotalCostExclVat,
                TotalCostInclVat = TotalCostInclVat,
                LastUpdated = LastUpdated,
                ChargingPeriods = ChargingPeriods.ToList(),
            };
        }
    }
}

/// <summary>Immutable snapshot of session mutable state.</summary>
public sealed class SessionSnapshot
{
    public required SessionPhase Phase { get; init; }
    public required DateTimeOffset? EndTime { get; init; }
    public required decimal KwhDelivered { get; init; }
    public required decimal TotalCostExclVat { get; init; }
    public required decimal TotalCostInclVat { get; init; }
    public required DateTimeOffset LastUpdated { get; init; }
    public required List<ChargingPeriodState> ChargingPeriods { get; init; }
}

public enum SessionPhase
{
    Pending,
    Active,
    Completed,
}

/// <summary>
/// Tracks a single charging period's accumulated dimensions.
/// </summary>
public sealed class ChargingPeriodState
{
    public required DateTimeOffset StartTime { get; init; }
    public decimal EnergyKwh { get; set; }
    public decimal TimeHours { get; set; }
    public decimal? MaxPowerKw { get; set; }
}
