namespace DotOcpi.Registry;

/// <summary>
/// Persistent backing store for CPO connections. Consumers implement
/// this interface to load connections from a database, file, or other
/// persistent storage at startup.
/// </summary>
public interface ICpoRegistryStore
{
    /// <summary>
    /// Loads all persisted CPO connections. Called once at startup to
    /// populate the in-memory <see cref="ICpoRegistry"/>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All persisted CPO connections.</returns>
    Task<IReadOnlyList<CpoConnection>> LoadAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a CPO connection. Called when a connection is added or updated.
    /// </summary>
    /// <param name="connection">The connection to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync(CpoConnection connection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a persisted CPO connection.
    /// </summary>
    /// <param name="connectionKey">The connection key to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveAsync(string connectionKey, CancellationToken cancellationToken = default);
}
