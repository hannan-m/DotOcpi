namespace DotOcpi.Client.Internal;

/// <summary>
/// Stores pending command/charging profile callbacks awaiting async results from CPOs.
/// </summary>
public interface ICallbackStore
{
    /// <summary>
    /// Stores a pending callback with a TTL.
    /// </summary>
    /// <param name="correlationId">Unique correlation ID for matching the callback.</param>
    /// <param name="callback">The pending callback data.</param>
    /// <param name="ttl">Time to live before the callback expires.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StoreAsync(
        string correlationId,
        PendingCallback callback,
        TimeSpan ttl,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Retrieves and removes a pending callback by correlation ID.
    /// Returns null if not found or already consumed.
    /// </summary>
    Task<PendingCallback?> GetAndRemoveAsync(string correlationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all callbacks that have exceeded their TTL.
    /// </summary>
    Task<IReadOnlyList<PendingCallback>> GetExpiredAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// A pending callback awaiting an async result from a CPO.
/// </summary>
/// <param name="CorrelationId">Unique correlation ID matching the response_url path.</param>
/// <param name="CpoId">The CPO connection key this callback belongs to.</param>
/// <param name="CommandType">The type of command or operation (e.g., "START_SESSION").</param>
/// <param name="CreatedAt">When the callback was stored.</param>
/// <param name="ExpiresAt">When the callback should be considered timed out.</param>
public sealed record PendingCallback(
    string CorrelationId,
    string CpoId,
    string CommandType,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt
);
