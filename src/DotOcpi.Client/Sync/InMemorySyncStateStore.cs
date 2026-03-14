using System.Collections.Concurrent;

namespace DotOcpi.Client.Sync;

/// <summary>
/// In-memory sync state store for single-instance deployments and development.
/// State is lost on restart. For production, implement <see cref="ISyncStateStore"/>
/// with persistent storage.
/// </summary>
internal sealed class InMemorySyncStateStore : ISyncStateStore
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _state = new();

    public Task<DateTimeOffset?> GetLastSyncAsync(
        string cpoId,
        string moduleId,
        CancellationToken cancellationToken = default
    )
    {
        var key = FormatKey(cpoId, moduleId);
        return Task.FromResult(_state.TryGetValue(key, out var timestamp) ? (DateTimeOffset?)timestamp : null);
    }

    public Task SetLastSyncAsync(
        string cpoId,
        string moduleId,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken = default
    )
    {
        _state[FormatKey(cpoId, moduleId)] = timestamp;
        return Task.CompletedTask;
    }

    private static string FormatKey(string cpoId, string moduleId) => $"{cpoId}:{moduleId}";
}
