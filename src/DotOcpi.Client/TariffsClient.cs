using DotOcpi.Client.Internal;

namespace DotOcpi.Client;

/// <summary>
/// Pulls Tariff data from CPO endpoints.
/// </summary>
internal sealed class TariffsClient : ITariffsClient
{
    private readonly HttpClient _httpClient;
    private readonly ICpoConnectionContextProvider _contextProvider;
    private readonly PaginationHandler _pagination;

    internal TariffsClient(HttpClient httpClient, ICpoConnectionContextProvider contextProvider)
    {
        _httpClient = httpClient;
        _contextProvider = contextProvider;
        _pagination = new PaginationHandler(httpClient);
    }

    public IAsyncEnumerable<object> GetAllTariffsAsync(
        string cpoId,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        CancellationToken cancellationToken = default
    ) => PullClientHelper.StreamAllAsync(
        _contextProvider, _pagination,
        cpoId, "tariffs", OcpiModelTypeMap.GetTariffType,
        dateFrom, dateTo, cancellationToken);
}
