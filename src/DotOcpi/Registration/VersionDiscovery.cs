using System.Net.Http.Json;
using DotOcpi.Exceptions;
using DotOcpi.Serialization;

namespace DotOcpi.Registration;

/// <summary>
/// Discovers available OCPI versions and endpoint details from a CPO
/// using HTTP requests to the versions and version detail endpoints.
/// </summary>
public sealed class VersionDiscovery : IVersionDiscovery
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of <see cref="VersionDiscovery"/>.
    /// </summary>
    /// <param name="httpClient">The HTTP client configured with appropriate auth headers.</param>
    public VersionDiscovery(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VersionInfo>> GetVersionsAsync(
        string versionsUrl,
        string token,
        CancellationToken cancellationToken = default
    )
    {
        using var request = CreateRequest(HttpMethod.Get, versionsUrl, token);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var ocpiResponse = await response
            .Content.ReadFromJsonAsync(
                OcpiRegistrationJsonContext.Default.OcpiResponseIReadOnlyListVersionInfo,
                cancellationToken
            )
            .ConfigureAwait(false);

        return ocpiResponse?.Data ?? throw new OcpiRegistrationException("CPO versions endpoint returned no data.");
    }

    /// <inheritdoc />
    public async Task<VersionDetailInfo> GetVersionDetailAsync(
        string versionDetailUrl,
        string token,
        CancellationToken cancellationToken = default
    )
    {
        using var request = CreateRequest(HttpMethod.Get, versionDetailUrl, token);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var ocpiResponse = await response
            .Content.ReadFromJsonAsync(
                OcpiRegistrationJsonContext.Default.OcpiResponseVersionDetailInfo,
                cancellationToken
            )
            .ConfigureAwait(false);

        return ocpiResponse?.Data
            ?? throw new OcpiRegistrationException("CPO version detail endpoint returned no data.");
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Token", token);
        return request;
    }
}
