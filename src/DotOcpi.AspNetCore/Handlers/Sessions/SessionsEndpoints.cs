using DotOcpi.AspNetCore.Filters;
using DotOcpi.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DotOcpi.AspNetCore.Handlers.Sessions;

/// <summary>
/// Maps OCPI Sessions module endpoints for all supported versions.
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
public static class SessionsEndpoints
{
    /// <summary>
    /// Maps Sessions receiver endpoints under the OCPI route group for all versions.
    /// </summary>
    public static RouteGroupBuilder MapSessionsEndpoints(this RouteGroupBuilder ocpiGroup)
    {
        foreach (var version in Enum.GetValues<OcpiVersion>())
        {
            var module = ocpiGroup.MapGroup($"{version.ToVersionString()}/sessions");
            module.AddEndpointFilter(new OcpiContextFilter("sessions"));
            module.AddEndpointFilter(new OcpiBodySizeLimitFilter(256 * 1024));
            var prefix = version.UsesPartyIdInUrls() ? "{countryCode}/{partyId}/" : "";

            module.MapPut($"{prefix}{{sessionId}}", HandleSessionPut);
            module.MapPatch($"{prefix}{{sessionId}}", HandleSessionPatch);
            module.MapGet($"{prefix}{{sessionId}}", HandleSessionGet);
        }

        return ocpiGroup;
    }

    internal static async Task HandleSessionPut(string sessionId, HttpContext httpContext)
    {
        var ctx = httpContext.GetOcpiContext()!;

        var data = await EndpointHelper
            .DeserializeOrRejectAsync(httpContext, ModuleHandlerFactory.Session(ctx.NegotiatedVersion))
            .ConfigureAwait(false);
        if (data is null)
            return;

        var receiver = httpContext.RequestServices.GetRequiredService<ISessionsReceiver>();
        var result = await receiver
            .OnSessionPutAsync(ctx, sessionId, data, httpContext.RequestAborted)
            .ConfigureAwait(false);

        await OcpiResponseWriter
            .WriteResultAsync(httpContext, result, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }

    internal static async Task HandleSessionPatch(string sessionId, HttpContext httpContext)
    {
        var ctx = httpContext.GetOcpiContext()!;

        var patch = await EndpointHelper.ReadPatchAsync(httpContext).ConfigureAwait(false);
        if (patch is null)
            return;

        var receiver = httpContext.RequestServices.GetRequiredService<ISessionsReceiver>();
        var result = await receiver
            .OnSessionPatchAsync(ctx, sessionId, patch.Value, httpContext.RequestAborted)
            .ConfigureAwait(false);

        await OcpiResponseWriter
            .WriteResultAsync(httpContext, result, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }

    internal static async Task HandleSessionGet(string sessionId, HttpContext httpContext)
    {
        var ctx = httpContext.GetOcpiContext()!;

        var receiver = httpContext.RequestServices.GetRequiredService<ISessionsReceiver>();
        var result = await receiver.GetSessionAsync(ctx, sessionId, httpContext.RequestAborted).ConfigureAwait(false);

        await OcpiResponseWriter
            .WriteResultObjectAsync(httpContext, result, ctx.NegotiatedVersion, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }
}
