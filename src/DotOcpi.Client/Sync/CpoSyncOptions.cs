namespace DotOcpi.Client.Sync;

/// <summary>
/// Per-CPO sync configuration override.
/// </summary>
public sealed class CpoSyncOptions
{
    /// <summary>Default sync interval for all modules from this CPO. Null uses global/module default.</summary>
    public TimeSpan? DefaultInterval { get; set; }

    /// <summary>Modules to sync for this CPO. Null uses the global enabled modules list.</summary>
    public List<string>? EnabledModules { get; set; }

    /// <summary>Per-module overrides specific to this CPO.</summary>
    public Dictionary<string, ModuleSyncOptions> ModuleOverrides { get; set; } = new();
}
