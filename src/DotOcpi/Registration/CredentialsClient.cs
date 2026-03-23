using System.Net.Http.Json;
using System.Text.Json;
using DotOcpi.Exceptions;
using DotOcpi.Serialization;

namespace DotOcpi.Registration;

/// <summary>
/// HTTP client for OCPI credentials operations (POST, PUT, DELETE)
/// against a CPO's credentials endpoint.
/// </summary>
public sealed class CredentialsClient : ICredentialsClient
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of <see cref="CredentialsClient"/>.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for requests.</param>
    public CredentialsClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<CredentialsResponse> PostCredentialsAsync(
        string credentialsUrl,
        string token,
        OcpiVersion version,
        object ourCredentials,
        CancellationToken cancellationToken = default
    )
    {
        return await SendCredentialsAsync(
                HttpMethod.Post,
                credentialsUrl,
                token,
                version,
                ourCredentials,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CredentialsResponse> PutCredentialsAsync(
        string credentialsUrl,
        string token,
        OcpiVersion version,
        object ourCredentials,
        CancellationToken cancellationToken = default
    )
    {
        return await SendCredentialsAsync(
                HttpMethod.Put,
                credentialsUrl,
                token,
                version,
                ourCredentials,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteCredentialsAsync(
        string credentialsUrl,
        string token,
        CancellationToken cancellationToken = default
    )
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, credentialsUrl);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Token", token);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    private async Task<CredentialsResponse> SendCredentialsAsync(
        HttpMethod method,
        string credentialsUrl,
        string token,
        OcpiVersion version,
        object ourCredentials,
        CancellationToken cancellationToken
    )
    {
        var options = OcpiJsonOptions.GetOptions(version);

        using var request = new HttpRequestMessage(method, credentialsUrl);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Token", token);
        request.Content = JsonContent.Create(ourCredentials, options.GetTypeInfo(ourCredentials.GetType()));

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument
            .ParseAsync(stream, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return ExtractCredentialsResponse(doc.RootElement, version);
    }

    /// <summary>
    /// Extracts a normalized <see cref="CredentialsResponse"/> from the raw
    /// JSON, handling both flat (2.0/2.1.1) and roles-based (2.2+) structures.
    /// </summary>
    private static CredentialsResponse ExtractCredentialsResponse(JsonElement root, OcpiVersion version)
    {
        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
        {
            throw new OcpiRegistrationException("CPO credentials response missing data object.");
        }

        var cpoToken = GetRequiredString(data, "token");
        var versionsUrl = GetRequiredString(data, "url");

        if (version.UsesPartyIdInUrls())
        {
            // 2.2+ uses roles array
            if (
                !data.TryGetProperty("roles", out var roles)
                || roles.ValueKind != JsonValueKind.Array
                || roles.GetArrayLength() == 0
            )
            {
                throw new OcpiRegistrationException("CPO credentials response missing roles array.");
            }

            var firstRole = roles[0];
            var countryCode = GetRequiredString(firstRole, "country_code");
            var partyId = GetRequiredString(firstRole, "party_id");

            return new CredentialsResponse(cpoToken, versionsUrl, countryCode, partyId);
        }

        // 2.0/2.1.1 uses flat structure
        return new CredentialsResponse(
            cpoToken,
            versionsUrl,
            GetRequiredString(data, "country_code"),
            GetRequiredString(data, "party_id")
        );
    }

    private static string GetRequiredString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString()!;
        }

        throw new OcpiRegistrationException($"CPO credentials response missing required field '{propertyName}'.");
    }
}
