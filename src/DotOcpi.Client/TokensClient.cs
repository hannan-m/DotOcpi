using System.Net.Http.Json;
using System.Text.Json;
using DotOcpi.Client.Internal;
using DotOcpi.Serialization;

namespace DotOcpi.Client;

/// <summary>
/// Pushes Token data to CPO endpoints.
/// </summary>
internal sealed class TokensClient : ITokensClient
{
    private readonly HttpClient _httpClient;
    private readonly ICpoConnectionContextProvider _contextProvider;

    internal TokensClient(HttpClient httpClient, ICpoConnectionContextProvider contextProvider)
    {
        _httpClient = httpClient;
        _contextProvider = contextProvider;
    }

    public async Task<OcpiResult> PushTokenAsync(
        string cpoId,
        string tokenUid,
        object token,
        CancellationToken cancellationToken = default
    )
    {
        var context = await _contextProvider.ResolveAsync(cpoId, cancellationToken).ConfigureAwait(false);
        var request = OcpiHttpRequestBuilder.Build(HttpMethod.Put, context, "tokens", tokenUid, token);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        return await OcpiResponseParser.ParseNoDataAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<OcpiResult> PatchTokenAsync(
        string cpoId,
        string tokenUid,
        JsonElement patch,
        CancellationToken cancellationToken = default
    )
    {
        var context = await _contextProvider.ResolveAsync(cpoId, cancellationToken).ConfigureAwait(false);
        var request = OcpiHttpRequestBuilder.Build(HttpMethod.Patch, context, "tokens", tokenUid);

        var options = OcpiJsonOptions.GetOptions(context.Connection.Version);
        request.Content = JsonContent.Create(patch, options.GetTypeInfo(typeof(JsonElement)));

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        return await OcpiResponseParser.ParseNoDataAsync(response, cancellationToken).ConfigureAwait(false);
    }
}
