namespace DotOcpi.Client.Sync;

/// <summary>
/// Per-module sync configuration override.
/// </summary>
public sealed class ModuleSyncOptions
{
    /// <summary>Sync interval for this module. Null uses the parent default.</summary>
    public TimeSpan? Interval { get; set; }
}
