namespace DotOcpi.Client.Sync;

/// <summary>
/// Tracks the last-sync timestamp per CPO per module for incremental pull synchronization.
/// Consumers implement this for persistent storage; in-memory implementation provided for dev use.
/// </summary>
public interface ISyncStateStore
{
    /// <summary>
    /// Gets the timestamp of the last successful sync for a CPO module.
    /// </summary>
    Task<DateTimeOffset?> GetLastSyncAsync(
        string cpoId,
        string moduleId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Records a successful sync timestamp for a CPO module.
    /// </summary>
    Task SetLastSyncAsync(
        string cpoId,
        string moduleId,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken = default
    );
}
