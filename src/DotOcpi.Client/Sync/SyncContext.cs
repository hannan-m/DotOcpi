namespace DotOcpi.Client.Sync;

/// <summary>
/// Context for a single sync operation, identifying the CPO, module,
/// negotiated version, and the time window being synced.
/// </summary>
public sealed class SyncContext
{
    /// <summary>The CPO connection key (e.g., "DE:ALL").</summary>
    public required string CpoId { get; init; }

    /// <summary>The module being synced (e.g., "locations").</summary>
    public required string ModuleId { get; init; }

    /// <summary>The OCPI version negotiated with this CPO.</summary>
    public required OcpiVersion Version { get; init; }

    /// <summary>Inclusive start of the sync window, or null for a full pull.</summary>
    public DateTimeOffset? DateFrom { get; init; }

    /// <summary>Timestamp when this sync cycle began (UTC).</summary>
    public required DateTimeOffset SyncStartedAt { get; init; }
}
