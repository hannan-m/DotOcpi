using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DotOcpi.Serialization;

namespace DotOcpi.Client.Internal;

/// <summary>
/// Builds outbound OCPI HTTP requests with auth headers, request IDs,
/// and version-specific JSON serialization. Stateless — all CPO-specific
/// data comes from the <see cref="CpoConnectionContext"/> passed by the caller.
/// </summary>
internal static class OcpiHttpRequestBuilder
{
    /// <summary>
    /// Creates an HTTP request for a CPO module endpoint.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="context">Pre-resolved connection context (registry + token).</param>
    /// <param name="moduleId">The OCPI module identifier (e.g., "locations").</param>
    /// <param name="pathSuffix">Optional path suffix appended after the module URL.</param>
    /// <param name="body">Optional body to serialize as JSON.</param>
    /// <returns>The constructed HTTP request message.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the module endpoint is not available for this CPO.
    /// </exception>
    internal static HttpRequestMessage Build(
        HttpMethod method,
        CpoConnectionContext context,
        string moduleId,
        string? pathSuffix,
        object? body = null
    )
    {
        if (!context.Connection.ModuleEndpoints.TryGetValue(moduleId, out var moduleUrl))
        {
            throw new InvalidOperationException(
                $"Module '{moduleId}' not available for CPO '{context.Connection.ConnectionKey}' (version {context.Connection.Version.ToVersionString()})."
            );
        }

        var url = pathSuffix is not null ? $"{moduleUrl.TrimEnd('/')}/{pathSuffix}" : moduleUrl;

        var request = new HttpRequestMessage(method, url);
        SetAuthHeader(request, context.RawToken);
        SetRequestHeaders(request);

        if (body is not null)
        {
            request.Content = SerializeBody(body, context.Connection.Version);
        }

        return request;
    }

    /// <summary>
    /// Creates an HTTP request for a CPO module endpoint with query parameters.
    /// </summary>
    internal static HttpRequestMessage BuildWithQuery(
        HttpMethod method,
        CpoConnectionContext context,
        string moduleId,
        string queryString
    )
    {
        if (!context.Connection.ModuleEndpoints.TryGetValue(moduleId, out var moduleUrl))
        {
            throw new InvalidOperationException(
                $"Module '{moduleId}' not available for CPO '{context.Connection.ConnectionKey}' (version {context.Connection.Version.ToVersionString()})."
            );
        }

        var separator = moduleUrl.Contains('?') ? "&" : "?";
        var url = $"{moduleUrl}{separator}{queryString}";

        var request = new HttpRequestMessage(method, url);
        SetAuthHeader(request, context.RawToken);
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

    private static void SetAuthHeader(HttpRequestMessage request, string cpoToken)
    {
        // Send the raw token as-is, consistent with CredentialsClient and
        // VersionDiscovery. The token (from IOutboundTokenProvider) is already
        // a base64url string produced by TokenGenerator. OCPI specifies
        // "Authorization: Token <token>" where the token is the string value
        // exchanged during the credentials handshake.
        request.Headers.Authorization = new AuthenticationHeaderValue("Token", cpoToken);
    }

    private static void SetRequestHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("X-Request-ID", Guid.NewGuid().ToString("D"));

        // Propagate an existing correlation ID from the ambient Activity (e.g.,
        // when this outbound call originates from an inbound ASP.NET Core request).
        // Falls back to a new GUID if there is no ambient trace context.
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString("D");
        request.Headers.Add("X-Correlation-ID", correlationId);
    }

    private static JsonContent SerializeBody(object body, OcpiVersion version)
    {
        var options = OcpiJsonOptions.GetOptions(version);
        return JsonContent.Create(body, options.GetTypeInfo(body.GetType()));
    }
}
