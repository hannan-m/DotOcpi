namespace DotOcpi.Registry;

/// <summary>
/// Notifies other instances when the CPO registry changes.
/// Used for multi-instance deployments where each instance maintains
/// its own in-memory registry and needs to stay synchronized.
/// </summary>
public interface ICacheInvalidationNotifier
{
    /// <summary>
    /// Notifies other instances that a CPO connection has changed.
    /// </summary>
    /// <param name="connectionKey">The connection key that changed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task NotifyChangedAsync(string connectionKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies other instances that a CPO connection has been removed.
    /// </summary>
    /// <param name="connectionKey">The connection key that was removed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task NotifyRemovedAsync(string connectionKey, CancellationToken cancellationToken = default);
}
