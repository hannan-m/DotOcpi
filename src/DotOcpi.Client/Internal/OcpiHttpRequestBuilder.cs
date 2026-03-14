using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DotOcpi.Registry;
using DotOcpi.Serialization;

namespace DotOcpi.Client.Internal;

/// <summary>
/// Builds outbound OCPI HTTP requests with auth headers, request IDs,
/// and version-specific JSON serialization.
/// </summary>
internal sealed class OcpiHttpRequestBuilder
{
    private readonly ICpoRegistry _registry;

    internal OcpiHttpRequestBuilder(ICpoRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>
    /// Creates an HTTP request for a CPO module endpoint.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="cpoId">The CPO connection key (e.g., "DE:ALL").</param>
    /// <param name="moduleId">The OCPI module identifier (e.g., "locations").</param>
    /// <param name="pathSuffix">Optional path suffix appended after the module URL.</param>
    /// <param name="cpoToken">The raw token issued by the CPO for authentication.</param>
    /// <param name="body">Optional body to serialize as JSON.</param>
    /// <returns>The constructed HTTP request message.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the CPO is not found in the registry or the module endpoint is not available.
    /// </exception>
    internal HttpRequestMessage Build(
        HttpMethod method,
        string cpoId,
        string moduleId,
        string? pathSuffix,
        string cpoToken,
        object? body = null
    )
    {
        var connection =
            _registry.FindByConnectionKey(cpoId)
            ?? throw new InvalidOperationException($"CPO '{cpoId}' not found in registry.");

        if (!connection.ModuleEndpoints.TryGetValue(moduleId, out var moduleUrl))
        {
            throw new InvalidOperationException(
                $"Module '{moduleId}' not available for CPO '{cpoId}' (version {connection.Version.ToVersionString()})."
            );
        }

        var url = pathSuffix is not null ? $"{moduleUrl.TrimEnd('/')}/{pathSuffix}" : moduleUrl;

        var request = new HttpRequestMessage(method, url);
        SetAuthHeader(request, cpoToken);
        SetRequestHeaders(request);

        if (body is not null)
        {
            request.Content = SerializeBody(body, connection.Version);
        }

        return request;
    }

    /// <summary>
    /// Creates an HTTP request for a CPO module endpoint with query parameters.
    /// </summary>
    internal HttpRequestMessage BuildWithQuery(
        HttpMethod method,
        string cpoId,
        string moduleId,
        string queryString,
        string cpoToken
    )
    {
        var connection =
            _registry.FindByConnectionKey(cpoId)
            ?? throw new InvalidOperationException($"CPO '{cpoId}' not found in registry.");

        if (!connection.ModuleEndpoints.TryGetValue(moduleId, out var moduleUrl))
        {
            throw new InvalidOperationException(
                $"Module '{moduleId}' not available for CPO '{cpoId}' (version {connection.Version.ToVersionString()})."
            );
        }

        var separator = moduleUrl.Contains('?') ? "&" : "?";
        var url = $"{moduleUrl}{separator}{queryString}";

        var request = new HttpRequestMessage(method, url);
        SetAuthHeader(request, cpoToken);
        SetRequestHeaders(request);

        return request;
    }

    /// <summary>
    /// Creates an HTTP request for an absolute URL (used for pagination Link headers).
    /// </summary>
    internal static HttpRequestMessage BuildForUrl(HttpMethod method, string absoluteUrl, string cpoToken)
    {
        var request = new HttpRequestMessage(method, absoluteUrl);
        SetAuthHeader(request, cpoToken);
        SetRequestHeaders(request);
        return request;
    }

    /// <summary>
    /// Gets the CPO connection for a given CPO ID.
    /// </summary>
    internal CpoConnection GetConnection(string cpoId) =>
        _registry.FindByConnectionKey(cpoId)
        ?? throw new InvalidOperationException($"CPO '{cpoId}' not found in registry.");

    private static void SetAuthHeader(HttpRequestMessage request, string cpoToken)
    {
        var tokenBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(cpoToken));
        request.Headers.Authorization = new AuthenticationHeaderValue("Token", tokenBase64);
    }

    private static void SetRequestHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("X-Request-ID", Guid.NewGuid().ToString("N"));
        request.Headers.Add("X-Correlation-ID", Guid.NewGuid().ToString("N"));
    }

    private static ByteArrayContent SerializeBody(object body, OcpiVersion version)
    {
        var options = OcpiJsonOptions.GetOptions(version);
        var json = JsonSerializer.SerializeToUtf8Bytes(body, body.GetType(), options);
        var content = new ByteArrayContent(json);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
        return content;
    }
}
