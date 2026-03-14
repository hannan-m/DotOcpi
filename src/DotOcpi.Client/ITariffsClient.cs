namespace DotOcpi.Client;

/// <summary>
/// Pulls Tariff data from a CPO's tariffs module endpoint.
/// Returns version-specific model types based on the CPO's negotiated version.
/// </summary>
public interface ITariffsClient
{
    /// <summary>
    /// Streams all tariffs from a CPO, following pagination Link headers.
    /// </summary>
    /// <param name="cpoId">The CPO connection key (e.g., "DE:ALL").</param>
    /// <param name="dateFrom">Only return objects modified after this timestamp (inclusive).</param>
    /// <param name="dateTo">Only return objects modified before this timestamp (exclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    IAsyncEnumerable<object> GetAllTariffsAsync(
        string cpoId,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        CancellationToken cancellationToken = default
    );
}
