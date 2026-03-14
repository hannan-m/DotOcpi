using System.Collections.Concurrent;

namespace DotOcpi.Client.Internal;

/// <summary>
/// In-memory callback store for single-instance deployments.
/// For multi-instance, consumers should implement <see cref="ICallbackStore"/> with a distributed store.
/// </summary>
internal sealed class InMemoryCallbackStore : ICallbackStore
{
    private readonly ConcurrentDictionary<string, PendingCallback> _callbacks = new();
    private readonly TimeProvider _timeProvider;

    internal InMemoryCallbackStore(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Task StoreAsync(
        string correlationId,
        PendingCallback callback,
        TimeSpan ttl,
        CancellationToken cancellationToken = default
    )
    {
        _callbacks[correlationId] = callback;
        return Task.CompletedTask;
    }

    public Task<PendingCallback?> GetAndRemoveAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        _callbacks.TryRemove(correlationId, out var callback);
        return Task.FromResult(callback);
    }

    public Task<IReadOnlyList<PendingCallback>> GetExpiredAsync(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var expired = new List<PendingCallback>();

        foreach (var (key, callback) in _callbacks)
        {
            if (callback.ExpiresAt <= now && _callbacks.TryRemove(key, out var removed))
            {
                expired.Add(removed);
            }
        }

        return Task.FromResult<IReadOnlyList<PendingCallback>>(expired);
    }
}
