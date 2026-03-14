namespace DotOcpi.Client.Sync;

/// <summary>
/// Configuration options for the pull synchronization background service.
/// </summary>
public sealed class PullSyncOptions
{
    /// <summary>Default interval between sync cycles.</summary>
    public TimeSpan DefaultInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>Modules to sync. Defaults to locations and tariffs.</summary>
    public IReadOnlyList<string> Modules { get; set; } = ["locations", "tariffs"];

    /// <summary>Random jitter added to the interval to prevent thundering herd.</summary>
    public TimeSpan RandomJitter { get; set; } = TimeSpan.FromMinutes(5);
}
