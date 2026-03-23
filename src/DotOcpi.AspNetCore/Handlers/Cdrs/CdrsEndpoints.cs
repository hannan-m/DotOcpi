using DotOcpi.AspNetCore.Filters;
using DotOcpi.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DotOcpi.AspNetCore.Handlers.Cdrs;

/// <summary>
/// Maps OCPI CDRs module endpoints for all supported versions.
/// CDRs use POST (not PUT) and return a Location header for the created resource.
/// Duplicate POSTs return the existing CDR for idempotency.
/// </summary>
[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
    "AOT",
    "IL2026:RequiresUnreferencedCode",
    Justification = "Endpoint delegates use only string and HttpContext parameters — no reflection-based binding."
)]
[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
    "AOT",
    "IL3050:RequiresDynamicCode",
    Justification = "Endpoint delegates use only string and HttpContext parameters — no runtime code generation needed."
)]
public static class CdrsEndpoints
{
    /// <summary>
    /// Maps CDR receiver endpoints under the OCPI route group for all versions.
    /// </summary>
    public static RouteGroupBuilder MapCdrsEndpoints(this RouteGroupBuilder ocpiGroup)
    {
        foreach (var version in Enum.GetValues<OcpiVersion>())
        {
            var module = ocpiGroup.MapGroup($"{version.ToVersionString()}/cdrs");
            module.AddEndpointFilter(new OcpiContextFilter("cdrs"));
            module.AddEndpointFilter(new OcpiBodySizeLimitFilter(1024 * 1024));

            module.MapPost("", HandleCdrPost);

            var prefix = version.UsesPartyIdInUrls() ? "{countryCode}/{partyId}/" : "";
            module.MapGet($"{prefix}{{cdrId}}", HandleCdrGet);
        }

        return ocpiGroup;
    }

    internal static async Task HandleCdrPost(HttpContext httpContext)
    {
        var ctx = httpContext.GetOcpiContext()!;

        var data = await EndpointHelper
            .DeserializeOrRejectAsync(httpContext, ModuleHandlerFactory.Cdr(ctx.NegotiatedVersion))
            .ConfigureAwait(false);
        if (data is null)
            return;

        var receiver = httpContext.RequestServices.GetRequiredService<ICdrsReceiver>();
        var result = await receiver.OnCdrPostAsync(ctx, data, httpContext.RequestAborted).ConfigureAwait(false);

        if (result.IsSuccess && result.Data is not null)
        {
            var cdrResult = result.Data;
            var basePath = httpContext.Request.PathBase + httpContext.Request.Path;
            var prefix = ctx.NegotiatedVersion.UsesPartyIdInUrls()
                ? $"{ctx.Connection.CpoCountryCode}/{ctx.Connection.CpoPartyId}/"
                : "";
            httpContext.Response.Headers["Location"] = $"{basePath}/{prefix}{cdrResult.CdrId}";

            if (!cdrResult.IsNew)
            {
                // Duplicate CDR — return the existing one
                httpContext.Response.StatusCode = StatusCodes.Status200OK;
            }
            else
            {
                httpContext.Response.StatusCode = StatusCodes.Status201Created;
            }

            await OcpiResponseWriter.WriteSuccessNoDataAsync(httpContext).ConfigureAwait(false);
        }
        else
        {
            await OcpiResponseWriter
                .WriteErrorAsync(
                    httpContext,
                    400,
                    result.StatusCode.Value,
                    result.StatusMessage ?? "CDR processing failed.",
                    httpContext.RequestAborted
                )
                .ConfigureAwait(false);
        }
    }

    internal static async Task HandleCdrGet(string cdrId, HttpContext httpContext)
    {
        var ctx = httpContext.GetOcpiContext()!;

        var receiver = httpContext.RequestServices.GetRequiredService<ICdrsReceiver>();
        var result = await receiver.GetCdrAsync(ctx, cdrId, httpContext.RequestAborted).ConfigureAwait(false);

        await OcpiResponseWriter
            .WriteResultObjectAsync(httpContext, result, ctx.NegotiatedVersion, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }
}
