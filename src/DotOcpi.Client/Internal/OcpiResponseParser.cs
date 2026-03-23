using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using DotOcpi.Serialization;

namespace DotOcpi.Client.Internal;

/// <summary>
/// Parses OCPI HTTP responses into <see cref="OcpiResult{T}"/> using stream-based deserialization.
/// Handles OCPI envelope extraction, status code mapping, and pagination header parsing.
/// </summary>
internal static partial class OcpiResponseParser
{
    [GeneratedRegex(@"<([^>]+)>;\s*rel=""next""", RegexOptions.IgnoreCase)]
    private static partial Regex LinkNextRegex();

    private static async Task<JsonDocument> ParseDocumentAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Parses an OCPI response containing a single data object.
    /// Parses the envelope via <see cref="JsonDocument"/> and deserializes
    /// only the <c>data</c> property as <typeparamref name="T"/>, so only
    /// the model type (not <c>OcpiResponse&lt;T&gt;</c>) needs to be in
    /// the source-generated context.
    /// </summary>
    internal static async Task<OcpiResult<T>> ParseObjectAsync<T>(
        HttpResponseMessage response,
        OcpiVersion version,
        CancellationToken cancellationToken = default
    )
    {
        if (!response.IsSuccessStatusCode)
        {
            return await ParseErrorAsync<T>(response, cancellationToken).ConfigureAwait(false);
        }

        var options = OcpiJsonOptions.GetOptions(version);
        using var doc = await ParseDocumentAsync(response, cancellationToken).ConfigureAwait(false);

        var root = doc.RootElement;
        var statusCode = new OcpiStatusCode(root.GetProperty("status_code").GetInt32());

        if (!statusCode.IsSuccess)
        {
            var message = root.TryGetProperty("status_message", out var msg) ? msg.GetString() : null;
            return OcpiResult<T>.Failure(statusCode, message ?? "Operation failed.");
        }

        if (!root.TryGetProperty("data", out var dataElement))
        {
            return OcpiResult<T>.Failure(statusCode, "Success response with null data.");
        }

        var data = (T?)dataElement.Deserialize(options.GetTypeInfo(typeof(T)));
        return data is not null
            ? OcpiResult<T>.Success(data)
            : OcpiResult<T>.Failure(OcpiStatusCode.GenericServerError, "Failed to deserialize response data.");
    }

    /// <summary>
    /// Parses an OCPI response containing a runtime-typed data object (deserialized as JsonElement).
    /// Used when the concrete model type depends on the negotiated version.
    /// </summary>
    internal static async Task<OcpiResult<object>> ParseVersionedObjectAsync(
        HttpResponseMessage response,
        OcpiVersion version,
        Type modelType,
        CancellationToken cancellationToken = default
    )
    {
        if (!response.IsSuccessStatusCode)
        {
            return await ParseErrorAsync<object>(response, cancellationToken).ConfigureAwait(false);
        }

        var options = OcpiJsonOptions.GetOptions(version);
        using var doc = await ParseDocumentAsync(response, cancellationToken).ConfigureAwait(false);

        var root = doc.RootElement;
        var statusCode = new OcpiStatusCode(root.GetProperty("status_code").GetInt32());

        if (!statusCode.IsSuccess)
        {
            var message = root.TryGetProperty("status_message", out var msg) ? msg.GetString() : null;
            return OcpiResult<object>.Failure(statusCode, message ?? "Operation failed.");
        }

        if (!root.TryGetProperty("data", out var dataElement))
        {
            return OcpiResult<object>.Failure(statusCode, "Success response with no data property.");
        }

        var data = dataElement.Deserialize(options.GetTypeInfo(modelType));
        return data is not null
            ? OcpiResult<object>.Success(data)
            : OcpiResult<object>.Failure(OcpiStatusCode.GenericServerError, "Failed to deserialize response data.");
    }

    /// <summary>
    /// Parses an OCPI response containing a list of runtime-typed data objects.
    /// </summary>
    internal static async Task<OcpiPageResult> ParseListAsync(
        HttpResponseMessage response,
        OcpiVersion version,
        Type itemType,
        CancellationToken cancellationToken = default
    )
    {
        if (!response.IsSuccessStatusCode)
        {
            var error = await ParseErrorAsync<object>(response, cancellationToken).ConfigureAwait(false);
            return new OcpiPageResult([], error.StatusCode, error.StatusMessage, 0, null);
        }

        var options = OcpiJsonOptions.GetOptions(version);
        using var doc = await ParseDocumentAsync(response, cancellationToken).ConfigureAwait(false);

        var root = doc.RootElement;
        var statusCode = new OcpiStatusCode(root.GetProperty("status_code").GetInt32());

        if (!statusCode.IsSuccess)
        {
            var message = root.TryGetProperty("status_message", out var msg) ? msg.GetString() : null;
            return new OcpiPageResult([], statusCode, message, 0, null);
        }

        var items = new List<object>();
        if (root.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in dataArray.EnumerateArray())
            {
                var item = element.Deserialize(options.GetTypeInfo(itemType));
                if (item is not null)
                    items.Add(item);
            }
        }

        var totalCount = ParseHeaderInt(response, "X-Total-Count");
        var nextLink = ParseLinkHeader(response);

        return new OcpiPageResult(items, statusCode, null, totalCount, nextLink);
    }

    /// <summary>
    /// Parses an OCPI response with no data payload (e.g., PUT/PATCH/DELETE responses).
    /// </summary>
    internal static async Task<OcpiResult> ParseNoDataAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken = default
    )
    {
        if (!response.IsSuccessStatusCode)
        {
            var error = await ParseErrorAsync<object>(response, cancellationToken).ConfigureAwait(false);
            return OcpiResult.Failure(error.StatusCode, error.StatusMessage ?? "Operation failed.");
        }

        using var doc = await ParseDocumentAsync(response, cancellationToken).ConfigureAwait(false);

        var statusCode = new OcpiStatusCode(doc.RootElement.GetProperty("status_code").GetInt32());

        if (statusCode.IsSuccess)
            return OcpiResult.Success();

        var message = doc.RootElement.TryGetProperty("status_message", out var msg) ? msg.GetString() : null;
        return OcpiResult.Failure(statusCode, message ?? "Operation failed.");
    }

    private static async Task<OcpiResult<T>> ParseErrorAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
        try
        {
            using var doc = await JsonDocument
                .ParseAsync(
                    await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);

            var root = doc.RootElement;
            var statusCode = root.TryGetProperty("status_code", out var sc)
                ? new OcpiStatusCode(sc.GetInt32())
                : MapHttpStatusToOcpi(response.StatusCode);
            var message = root.TryGetProperty("status_message", out var msg) ? msg.GetString() : null;

            return OcpiResult<T>.Failure(statusCode, message ?? $"HTTP {(int)response.StatusCode}");
        }
        catch (JsonException)
        {
            return OcpiResult<T>.Failure(
                MapHttpStatusToOcpi(response.StatusCode),
                $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}"
            );
        }
    }

    private static OcpiStatusCode MapHttpStatusToOcpi(HttpStatusCode httpStatus) =>
        (int)httpStatus switch
        {
            >= 500 => OcpiStatusCode.GenericServerError,
            401 => new OcpiStatusCode(2002),
            _ => OcpiStatusCode.GenericClientError,
        };

    private static int ParseHeaderInt(HttpResponseMessage response, string headerName)
    {
        if (
            response.Headers.TryGetValues(headerName, out var values)
            && int.TryParse(values.FirstOrDefault(), out var result)
        )
        {
            return result;
        }
        return 0;
    }

    internal static string? ParseLinkHeader(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Link", out var values))
            return null;

        var link = values.FirstOrDefault();
        if (link is null)
            return null;

        var match = LinkNextRegex().Match(link);
        return match.Success ? match.Groups[1].Value : null;
    }
}

/// <summary>
/// A page of results from a paginated OCPI GET endpoint.
/// </summary>
/// <param name="Items">The items on this page.</param>
/// <param name="StatusCode">The OCPI status code.</param>
/// <param name="StatusMessage">Optional status message.</param>
/// <param name="TotalCount">Total count from X-Total-Count header, or 0 if absent.</param>
/// <param name="NextLink">URL to the next page from Link header, or null if this is the last page.</param>
internal sealed record OcpiPageResult(
    IReadOnlyList<object> Items,
    OcpiStatusCode StatusCode,
    string? StatusMessage,
    int TotalCount,
    string? NextLink
);
