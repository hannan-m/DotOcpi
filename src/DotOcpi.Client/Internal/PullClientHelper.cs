using System.Runtime.CompilerServices;

namespace DotOcpi.Client.Internal;

/// <summary>
/// Shared pagination logic for pull-client GetAll methods.
/// Eliminates duplicated context resolution, query building, and
/// pagination streaming across LocationsClient, SessionsClient,
/// CdrsClient, and TariffsClient.
/// </summary>
internal static class PullClientHelper
{
    internal static async IAsyncEnumerable<object> StreamAllAsync(
        ICpoConnectionContextProvider contextProvider,
        PaginationHandler pagination,
        string cpoId,
        string moduleId,
        Func<OcpiVersion, Type> typeSelector,
        DateTimeOffset? dateFrom,
        DateTimeOffset? dateTo,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var context = await contextProvider.ResolveAsync(cpoId, cancellationToken).ConfigureAwait(false);
        var modelType = typeSelector(context.Connection.Version);

        var query = QueryStringBuilder.BuildDateFilter(dateFrom, dateTo);
        var request = query is not null
            ? OcpiHttpRequestBuilder.BuildWithQuery(HttpMethod.Get, context, moduleId, query)
            : OcpiHttpRequestBuilder.Build(HttpMethod.Get, context, moduleId, null);

        await foreach (
            var item in pagination
                .StreamAllAsync(request, context.Connection.Version, modelType, context.RawToken, cancellationToken)
                .ConfigureAwait(false)
        )
        {
            yield return item;
        }
    }

    internal static async IAsyncEnumerable<OcpiPageResult> StreamPagesAsync(
        ICpoConnectionContextProvider contextProvider,
        PaginationHandler pagination,
        string cpoId,
        string moduleId,
        Func<OcpiVersion, Type> typeSelector,
        DateTimeOffset? dateFrom,
        DateTimeOffset? dateTo,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var context = await contextProvider.ResolveAsync(cpoId, cancellationToken).ConfigureAwait(false);
        var modelType = typeSelector(context.Connection.Version);

        var query = QueryStringBuilder.BuildDateFilter(dateFrom, dateTo);
        var request = query is not null
            ? OcpiHttpRequestBuilder.BuildWithQuery(HttpMethod.Get, context, moduleId, query)
            : OcpiHttpRequestBuilder.Build(HttpMethod.Get, context, moduleId, null);

        await foreach (
            var page in pagination
                .StreamPagesAsync(request, context.Connection.Version, modelType, context.RawToken, cancellationToken)
                .ConfigureAwait(false)
        )
        {
            yield return page;
        }
    }
}
