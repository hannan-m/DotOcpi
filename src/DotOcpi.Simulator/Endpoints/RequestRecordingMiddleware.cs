using System.Text.Json;
using DotOcpi.Simulator.State;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace DotOcpi.Simulator.Endpoints;

/// <summary>
/// Middleware that records all HTTP requests for test inspection.
/// Also parses JSON bodies and stores them in HttpContext.Items for handlers.
/// </summary>
internal static class RequestRecordingMiddleware
{
    internal static readonly object BodyItemKey = new();
    private static readonly JsonSerializerOptions SafeJsonOptions = new() { MaxDepth = 32 };

    /// <summary>Gets the parsed JSON body from the request, or null if not available or not a JSON object.</summary>
    internal static JsonElement? GetParsedObjectBody(HttpContext ctx)
    {
        if (
            ctx.Items.TryGetValue(BodyItemKey, out var body)
            && body is JsonElement el
            && el.ValueKind == JsonValueKind.Object
        )
            return el;
        return null;
    }

    /// <summary>Gets the parsed JSON body from the request (any value kind), or null.</summary>
    internal static JsonElement? GetParsedBody(HttpContext ctx)
    {
        if (ctx.Items.TryGetValue(BodyItemKey, out var body) && body is JsonElement el)
            return el;
        return null;
    }

    public static void Use(IApplicationBuilder app, SimulatorState state)
    {
        app.Use(
            async (ctx, next) =>
            {
                ctx.Request.EnableBuffering();

                JsonElement? body = null;
                if (ctx.Request.ContentLength > 0 || ctx.Request.ContentType?.Contains("json") == true)
                {
                    try
                    {
                        body = await JsonSerializer
                            .DeserializeAsync<JsonElement>(
                                ctx.Request.Body,
                                SafeJsonOptions,
                                cancellationToken: ctx.RequestAborted
                            )
                            .ConfigureAwait(false);
                        ctx.Request.Body.Position = 0;
                    }
                    catch (JsonException)
                    {
                        ctx.Request.Body.Position = 0;
                    }
                }

                ctx.Items[BodyItemKey] = body;

                var headers = new Dictionary<string, string>();
                foreach (var header in ctx.Request.Headers)
                    headers[header.Key] = header.Value.ToString();

                state.TrimRequestHistory();

                // Capture response status after handler runs
                await next(ctx).ConfigureAwait(false);

                state.RequestHistory.Enqueue(
                    new RecordedRequest
                    {
                        Method = ctx.Request.Method,
                        Path = ctx.Request.Path.Value ?? "",
                        QueryString = ctx.Request.QueryString.HasValue ? ctx.Request.QueryString.Value : null,
                        Headers = headers,
                        Body = body,
                        Timestamp = DateTimeOffset.UtcNow,
                        ResponseStatusCode = ctx.Response.StatusCode,
                    }
                );
            }
        );
    }
}
