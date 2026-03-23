namespace DotOcpi.Client.Sync;

/// <summary>
/// Outcome of a completed sync operation for a single CPO+module pair.
/// </summary>
public sealed class SyncResult
{
    /// <summary>Total number of items received across all pages.</summary>
    public required int ItemCount { get; init; }

    /// <summary>Number of pages fetched.</summary>
    public required int PageCount { get; init; }

    /// <summary>Wall-clock duration of the sync operation.</summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>True if the sync completed without errors.</summary>
    public required bool IsSuccess { get; init; }

    /// <summary>Error message if the sync failed, null otherwise.</summary>
    public string? ErrorMessage { get; init; }
}
