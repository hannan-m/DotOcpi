namespace DotOcpi.Client.Sync;

/// <summary>
/// Service for pulling data from CPOs. Can be invoked manually or by a background service.
/// </summary>
public interface IOcpiSyncService
{
    /// <summary>
    /// Synchronizes all configured modules from a specific CPO.
    /// Uses the last-sync timestamp from <see cref="ISyncStateStore"/> for incremental pulls.
    /// </summary>
    Task SyncFromCpoAsync(string cpoId, DateTimeOffset? since = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronizes a specific module from a specific CPO.
    /// Returns a <see cref="SyncResult"/> describing the outcome.
    /// </summary>
    Task<SyncResult> SyncModuleFromCpoAsync(
        string cpoId,
        string moduleId,
        DateTimeOffset? since = null,
        CancellationToken cancellationToken = default
    );
}
