namespace DotOcpi.Client;

/// <summary>
/// Pulls Location data from a CPO's locations module endpoint.
/// Returns version-specific model types based on the CPO's negotiated version.
/// </summary>
public interface ILocationsClient
{
    /// <summary>
    /// Gets a single location by ID from a CPO.
    /// </summary>
    /// <param name="cpoId">The CPO connection key (e.g., "DE:ALL").</param>
    /// <param name="locationId">The location identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The location as a version-specific model type.</returns>
    Task<OcpiResult<object>> GetLocationAsync(
        string cpoId,
        string locationId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Streams all locations from a CPO, following pagination Link headers.
    /// </summary>
    /// <param name="cpoId">The CPO connection key (e.g., "DE:ALL").</param>
    /// <param name="dateFrom">Only return objects modified after this timestamp (inclusive).</param>
    /// <param name="dateTo">Only return objects modified before this timestamp (exclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    IAsyncEnumerable<object> GetAllLocationsAsync(
        string cpoId,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        CancellationToken cancellationToken = default
    );
}
