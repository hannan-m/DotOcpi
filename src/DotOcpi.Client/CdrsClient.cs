using DotOcpi.Client.Internal;

namespace DotOcpi.Client;

/// <summary>
/// Pulls CDR (Charge Detail Record) data from CPO endpoints.
/// </summary>
internal sealed class CdrsClient : ICdrsClient
{
    private readonly HttpClient _httpClient;
    private readonly ICpoConnectionContextProvider _contextProvider;
    private readonly PaginationHandler _pagination;

    internal CdrsClient(HttpClient httpClient, ICpoConnectionContextProvider contextProvider)
    {
        _httpClient = httpClient;
        _contextProvider = contextProvider;
        _pagination = new PaginationHandler(httpClient);
    }

    public IAsyncEnumerable<object> GetAllCdrsAsync(
        string cpoId,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        CancellationToken cancellationToken = default
    ) =>
        PullClientHelper.StreamAllAsync(
            _contextProvider,
            _pagination,
            cpoId,
            "cdrs",
            OcpiModelTypeMap.GetCdrType,
            dateFrom,
            dateTo,
            cancellationToken
        );
}
