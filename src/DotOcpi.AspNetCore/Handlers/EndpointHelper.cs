using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace DotOcpi.AspNetCore.Handlers;

/// <summary>
/// Shared helpers for OCPI module endpoint handlers.
/// </summary>
internal static class EndpointHelper
{
    /// <summary>
    /// Deserializes the request body using the given handler.
    /// Returns null and writes a 400 error if the body is missing or invalid.
    /// </summary>
    internal static async ValueTask<object?> DeserializeOrRejectAsync(HttpContext httpContext, IModuleHandler handler)
    {
        try
        {
            var data = await handler
                .DeserializeAsync(httpContext.Request.Body, httpContext.RequestAborted)
                .ConfigureAwait(false);
            if (data is not null)
                return data;

            await OcpiResponseWriter
                .WriteErrorAsync(
                    httpContext,
                    400,
                    OcpiStatusCode.InvalidParameters.Value,
                    "Request body is required.",
                    httpContext.RequestAborted
                )
                .ConfigureAwait(false);
            return null;
        }
        catch (JsonException)
        {
            await OcpiResponseWriter
                .WriteErrorAsync(
                    httpContext,
                    400,
                    OcpiStatusCode.InvalidParameters.Value,
                    "Request body contains invalid JSON.",
                    httpContext.RequestAborted
                )
                .ConfigureAwait(false);
            return null;
        }
    }

    /// <summary>
    /// Reads the request body as a <see cref="JsonElement"/> for PATCH operations.
    /// Returns null and writes a 400 error if the body is missing or invalid.
    /// </summary>
    internal static async ValueTask<JsonElement?> ReadPatchAsync(HttpContext httpContext)
    {
        if (httpContext.Request.ContentLength == 0)
        {
            await OcpiResponseWriter
                .WriteErrorAsync(
                    httpContext,
                    400,
                    OcpiStatusCode.InvalidParameters.Value,
                    "Request body is required.",
                    httpContext.RequestAborted
                )
                .ConfigureAwait(false);
            return null;
        }

        try
        {
            using var doc = await JsonDocument
                .ParseAsync(httpContext.Request.Body, cancellationToken: httpContext.RequestAborted)
                .ConfigureAwait(false);
            var element = doc.RootElement.Clone();

            if (element.ValueKind != JsonValueKind.Object)
            {
                await OcpiResponseWriter
                    .WriteErrorAsync(
                        httpContext,
                        400,
                        OcpiStatusCode.InvalidParameters.Value,
                        "PATCH body must be a JSON object.",
                        httpContext.RequestAborted
                    )
                    .ConfigureAwait(false);
                return null;
            }

            return element;
        }
        catch (JsonException)
        {
            await OcpiResponseWriter
                .WriteErrorAsync(
                    httpContext,
                    400,
                    OcpiStatusCode.InvalidParameters.Value,
                    "Request body contains invalid JSON.",
                    httpContext.RequestAborted
                )
                .ConfigureAwait(false);
            return null;
        }
    }
}
