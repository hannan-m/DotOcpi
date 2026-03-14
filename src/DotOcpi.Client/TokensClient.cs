using System.Net.Http.Headers;
using System.Text.Json;
using DotOcpi.Client.Internal;

namespace DotOcpi.Client;

/// <summary>
/// Pushes Token data to CPO endpoints.
/// </summary>
internal sealed class TokensClient : ITokensClient
{
    private readonly HttpClient _httpClient;
    private readonly OcpiHttpRequestBuilder _requestBuilder;
    private readonly IOutboundTokenProvider _tokenProvider;

    internal TokensClient(
        HttpClient httpClient,
        OcpiHttpRequestBuilder requestBuilder,
        IOutboundTokenProvider tokenProvider
    )
    {
        _httpClient = httpClient;
        _requestBuilder = requestBuilder;
        _tokenProvider = tokenProvider;
    }

    public async Task<OcpiResult> PushTokenAsync(
        string cpoId,
        string tokenUid,
        object token,
        CancellationToken cancellationToken = default
    )
    {
        var cpoToken = await _tokenProvider.GetTokenAsync(cpoId, cancellationToken).ConfigureAwait(false);
        var request = _requestBuilder.Build(HttpMethod.Put, cpoId, "tokens", tokenUid, cpoToken, token);

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
        var connection = _requestBuilder.GetConnection(cpoId);
        var cpoToken = await _tokenProvider.GetTokenAsync(cpoId, cancellationToken).ConfigureAwait(false);

        var request = _requestBuilder.Build(HttpMethod.Patch, cpoId, "tokens", tokenUid, cpoToken);

        // Serialize the JsonElement directly as the request body
        var json = JsonSerializer.SerializeToUtf8Bytes(patch);
        request.Content = new ByteArrayContent(json);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        return await OcpiResponseParser.ParseNoDataAsync(response, cancellationToken).ConfigureAwait(false);
    }
}
