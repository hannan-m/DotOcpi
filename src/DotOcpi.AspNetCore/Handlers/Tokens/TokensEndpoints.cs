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
        var offset = ParseInt(httpContext.Request.Query["offset"], 0);
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
            httpContext.Response.Headers["Link"] = $"<{basePath}?offset={nextOffset}&limit={limit}>; rel=\"next\"";
        }

        await OcpiResponseWriter
            .WriteSuccessListAsync(httpContext, result.Items, ctx.NegotiatedVersion, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }

    internal static async Task HandleTokenAuthorize(HttpContext httpContext)
    {
        var ctx = httpContext.GetOcpiContext()!;
        var tokenUid = (string)httpContext.GetRouteValue("tokenUid")!;

        // Body is optional — LocationReferences may be null
        object? locationReferences = null;
        if (httpContext.Request.ContentLength > 0 || httpContext.Request.ContentType is not null)
        {
            locationReferences = await EndpointHelper
                .DeserializeOrRejectAsync(httpContext, ModuleHandlerFactory.LocationReferences(ctx.NegotiatedVersion))
                .ConfigureAwait(false);
            // DeserializeOrRejectAsync returns null for invalid body AND empty body.
            // For authorize, empty body is valid (no location filter), but invalid JSON is not.
            // We need to distinguish: if Content-Type was set but deserialization failed, it's an error.
            if (locationReferences is null && httpContext.Response.HasStarted)
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
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result)
            ? result
            : null;

    private static int ParseInt(string? value, int defaultValue) =>
        int.TryParse(value, CultureInfo.InvariantCulture, out var result) ? result : defaultValue;
}
