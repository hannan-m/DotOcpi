using DotOcpi.AspNetCore.Filters;
using DotOcpi.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DotOcpi.AspNetCore.Handlers.Tariffs;

/// <summary>
/// Maps OCPI Tariffs module endpoints for all supported versions.
/// PATCH is only supported for 2.0/2.1.1; 2.2+ rejects PATCH with 405.
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
public static class TariffsEndpoints
{
    /// <summary>
    /// Maps Tariff receiver endpoints under the OCPI route group for all versions.
    /// </summary>
    public static RouteGroupBuilder MapTariffsEndpoints(this RouteGroupBuilder ocpiGroup)
    {
        foreach (var version in Enum.GetValues<OcpiVersion>())
        {
            var module = ocpiGroup.MapGroup($"{version.ToVersionString()}/tariffs");
            module.AddEndpointFilter(new OcpiContextFilter("tariffs"));
            module.AddEndpointFilter(new OcpiBodySizeLimitFilter(256 * 1024));
            var prefix = version.UsesPartyIdInUrls() ? "{countryCode}/{partyId}/" : "";

            module.MapPut($"{prefix}{{tariffId}}", HandleTariffPut);
            module.MapGet($"{prefix}{{tariffId}}", HandleTariffGet);
            module.MapDelete($"{prefix}{{tariffId}}", HandleTariffDelete);

            if (version.UsesPartyIdInUrls())
            {
                // 2.2+: PATCH returns 405 Method Not Allowed
                module.MapPatch($"{prefix}{{tariffId}}", HandleTariffPatchRejected);
            }
            else
            {
                // 2.0/2.1.1: PATCH is supported
                module.MapPatch($"{prefix}{{tariffId}}", HandleTariffPatch);
            }
        }

        return ocpiGroup;
    }

    internal static async Task HandleTariffPut(string tariffId, HttpContext httpContext)
    {
        var ctx = httpContext.GetOcpiContext()!; // Safe: OcpiContextFilter runs before handler

        var data = await EndpointHelper
            .DeserializeOrRejectAsync(httpContext, ModuleHandlerFactory.Tariff(ctx.NegotiatedVersion))
            .ConfigureAwait(false);
        if (data is null)
            return;

        var receiver = httpContext.RequestServices.GetRequiredService<ITariffsReceiver>();
        var result = await receiver
            .OnTariffPutAsync(ctx, tariffId, data, httpContext.RequestAborted)
            .ConfigureAwait(false);

        await OcpiResponseWriter
            .WriteResultAsync(httpContext, result, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }

    internal static async Task HandleTariffPatch(string tariffId, HttpContext httpContext)
    {
        var ctx = httpContext.GetOcpiContext()!; // Safe: OcpiContextFilter runs before handler

        var patch = await EndpointHelper.ReadPatchAsync(httpContext).ConfigureAwait(false);
        if (patch is null)
            return;

        var receiver = httpContext.RequestServices.GetRequiredService<ITariffsReceiver>();
        var result = await receiver
            .OnTariffPatchAsync(ctx, tariffId, patch.Value, httpContext.RequestAborted)
            .ConfigureAwait(false);

        await OcpiResponseWriter
            .WriteResultAsync(httpContext, result, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }

    internal static Task HandleTariffPatchRejected(HttpContext httpContext)
    {
        return OcpiResponseWriter.WriteErrorAsync(
            httpContext,
            StatusCodes.Status405MethodNotAllowed,
            OcpiStatusCode.InvalidParameters.Value,
            "PATCH is not supported for Tariffs in OCPI 2.2+. Use PUT to replace the full tariff.",
            httpContext.RequestAborted
        );
    }

    internal static async Task HandleTariffDelete(string tariffId, HttpContext httpContext)
    {
        var ctx = httpContext.GetOcpiContext()!; // Safe: OcpiContextFilter runs before handler

        var receiver = httpContext.RequestServices.GetRequiredService<ITariffsReceiver>();
        var result = await receiver
            .OnTariffDeleteAsync(ctx, tariffId, httpContext.RequestAborted)
            .ConfigureAwait(false);

        await OcpiResponseWriter
            .WriteResultAsync(httpContext, result, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }

    internal static async Task HandleTariffGet(string tariffId, HttpContext httpContext)
    {
        var ctx = httpContext.GetOcpiContext()!; // Safe: OcpiContextFilter runs before handler

        var receiver = httpContext.RequestServices.GetRequiredService<ITariffsReceiver>();
        var result = await receiver.GetTariffAsync(ctx, tariffId, httpContext.RequestAborted).ConfigureAwait(false);

        await OcpiResponseWriter
            .WriteResultObjectAsync(httpContext, result, ctx.NegotiatedVersion, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }
}
