namespace DotOcpi.Client.Sync;

/// <summary>
/// Configuration for the pull synchronization background service.
/// Supports global defaults, per-module overrides, and per-CPO overrides.
/// Resolution order: CPO+module → CPO default → module-level → global default.
/// </summary>
public sealed class PullSyncOptions
{
    /// <summary>Default interval between sync cycles for all modules and CPOs.</summary>
    public TimeSpan DefaultInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Modules enabled for pull sync. Only modules listed here are synced
    /// unless overridden per-CPO. Valid: "locations", "sessions", "cdrs", "tariffs".
    /// </summary>
    public List<string> EnabledModules { get; set; } = ["locations", "tariffs"];

    /// <summary>Maximum random jitter added to intervals to prevent thundering herd.</summary>
    public TimeSpan MaxJitter { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Per-module overrides. Key is the module ID (e.g., "locations").</summary>
    public Dictionary<string, ModuleSyncOptions> ModuleOverrides { get; set; } = new();

    /// <summary>Per-CPO overrides. Key is the CPO connection key (e.g., "DE:ALL").</summary>
    public Dictionary<string, CpoSyncOptions> CpoOverrides { get; set; } = new();
}
