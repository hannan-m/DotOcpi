namespace DotOcpi.Client.Sync;

/// <summary>
/// Consumer-provided callback that receives pulled OCPI data during sync.
/// Called by <see cref="IOcpiSyncService"/> with page-level batches to allow
/// efficient bulk persistence without single-item processing overhead.
/// </summary>
public interface IOcpiSyncHandler
{
    /// <summary>
    /// Called once per page of results fetched from a CPO module endpoint.
    /// Items are version-specific model types (e.g., <c>Models.V2_2_1.Location</c>).
    /// Pages with zero items are not delivered.
    /// </summary>
    Task OnPageReceivedAsync(
        SyncContext context,
        IReadOnlyList<object> items,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Called after all pages for a CPO+module sync cycle have been successfully
    /// fetched and delivered. Not called if the sync fails or is cancelled.
    /// </summary>
    Task OnSyncCompletedAsync(
        SyncContext context,
        SyncResult result,
        CancellationToken cancellationToken = default
    );
}
