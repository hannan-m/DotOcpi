using System.Globalization;
using DotOcpi.AspNetCore.Filters;
using DotOcpi.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DotOcpi.AspNetCore.Handlers.Tokens;

/// <summary>
/// Maps OCPI Tokens module endpoints for all supported versions.
/// eMSP is the Sender: serves token list via GET and handles POST authorize.
/// </summary>
[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
    "AOT", "IL2026:RequiresUnreferencedCode",
    Justification = "Endpoint delegates use only string and HttpContext parameters — no reflection-based binding.")]
[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
    "AOT", "IL3050:RequiresDynamicCode",
    Justification = "Endpoint delegates use only string and HttpContext parameters — no runtime code generation needed.")]
public static class TokensEndpoints
{
    private const int DefaultLimit = 50;
    private const int MaxLimit = 1000;

    /// <summary>
    /// Maps Tokens sender endpoints under the OCPI route group for all versions.
    /// </summary>
    public static RouteGroupBuilder MapTokensEndpoints(this RouteGroupBuilder ocpiGroup)
    {
        foreach (var version in Enum.GetValues<OcpiVersion>())
        {
            var module = ocpiGroup.MapGroup($"{version.ToVersionString()}/tokens");
            module.AddEndpointFilter(new OcpiContextFilter("tokens"));
            module.AddEndpointFilter(new OcpiBodySizeLimitFilter(8 * 1024));

            module.MapGet("", HandleTokensGet);

            var prefix = version.UsesPartyIdInUrls() ? "{countryCode}/{partyId}/" : "";
            module.MapPost($"{prefix}{{tokenUid}}/authorize", HandleTokenAuthorize);
        }

        return ocpiGroup;
    }

    internal static async Task HandleTokensGet(HttpContext httpContext)
    {
        var ctx = httpContext.GetOcpiContext()!;

        var dateFrom = ParseDateTimeOffset(httpContext.Request.Query["date_from"]);
        var dateTo = ParseDateTimeOffset(httpContext.Request.Query["date_to"]);
        var offset = Math.Max(0, ParseInt(httpContext.Request.Query["offset"], 0));
        var limit = Math.Clamp(ParseInt(httpContext.Request.Query["limit"], DefaultLimit), 1, MaxLimit);

        var sender = httpContext.RequestServices.GetRequiredService<ITokensSender>();
        var result = await sender
            .GetTokensAsync(ctx, dateFrom, dateTo, offset, limit, httpContext.RequestAborted)
            .ConfigureAwait(false);

        httpContext.Response.Headers["X-Total-Count"] = result.TotalCount.ToString(CultureInfo.InvariantCulture);
        httpContext.Response.Headers["X-Limit"] = result.Limit.ToString(CultureInfo.InvariantCulture);

        if (offset + result.Items.Count < result.TotalCount)
        {
            var nextOffset = offset + result.Items.Count;
            var basePath = httpContext.Request.PathBase + httpContext.Request.Path;
            var linkQuery = $"offset={nextOffset}&limit={limit}";
            if (httpContext.Request.Query.ContainsKey("date_from"))
                linkQuery += $"&date_from={httpContext.Request.Query["date_from"]}";
            if (httpContext.Request.Query.ContainsKey("date_to"))
                linkQuery += $"&date_to={httpContext.Request.Query["date_to"]}";
            httpContext.Response.Headers["Link"] = $"<{basePath}?{linkQuery}>; rel=\"next\"";
        }

        await OcpiResponseWriter
            .WriteSuccessListAsync(httpContext, result.Items, ctx.NegotiatedVersion, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }

    internal static async Task HandleTokenAuthorize(string tokenUid, HttpContext httpContext)
    {
        var ctx = httpContext.GetOcpiContext()!;

        // Body is optional — LocationReferences may be null for authorization without location filter
        object? locationReferences = null;
        if (httpContext.Request.ContentLength > 0 || httpContext.Request.ContentType is not null)
        {
            var handler = ModuleHandlerFactory.LocationReferences(ctx.NegotiatedVersion);
            locationReferences = await EndpointHelper
                .DeserializeOrRejectAsync(httpContext, handler)
                .ConfigureAwait(false);
            if (locationReferences is null)
                return;
        }

        var authorizer = httpContext.RequestServices.GetRequiredService<ITokensAuthorizer>();
        var result = await authorizer
            .AuthorizeAsync(ctx, tokenUid, locationReferences, httpContext.RequestAborted)
            .ConfigureAwait(false);

        await OcpiResponseWriter
            .WriteResultObjectAsync(httpContext, result, ctx.NegotiatedVersion, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }

    private static DateTimeOffset? ParseDateTimeOffset(string? value) =>
        DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var result
        )
            ? result
            : null;

    private static int ParseInt(string? value, int defaultValue) =>
        int.TryParse(value, CultureInfo.InvariantCulture, out var result) ? result : defaultValue;
}
