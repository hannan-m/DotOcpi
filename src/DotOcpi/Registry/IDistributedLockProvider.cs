namespace DotOcpi.Registry;

/// <summary>
/// Provides distributed locks for operations that must be serialized
/// across multiple instances (e.g., credential rotation, registration).
/// </summary>
public interface IDistributedLockProvider
{
    /// <summary>
    /// Acquires a distributed lock for the given resource key.
    /// </summary>
    /// <param name="resourceKey">The resource to lock (e.g., connection key).</param>
    /// <param name="timeout">Maximum time to wait for the lock.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A disposable lock handle; dispose to release.</returns>
    Task<IAsyncDisposable> AcquireAsync(
        string resourceKey,
        TimeSpan timeout,
        CancellationToken cancellationToken = default
    );
}
