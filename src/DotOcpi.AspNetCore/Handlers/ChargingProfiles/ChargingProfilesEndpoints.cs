using DotOcpi.AspNetCore.Filters;
using DotOcpi.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DotOcpi.AspNetCore.Handlers.ChargingProfiles;

/// <summary>
/// Maps OCPI ChargingProfiles module callback endpoints.
/// Only available for OCPI 2.2 and 2.2.1 (not supported in earlier versions).
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
public static class ChargingProfilesEndpoints
{
    /// <summary>
    /// Maps ChargingProfiles callback endpoints under the OCPI route group.
    /// </summary>
    public static RouteGroupBuilder MapChargingProfilesEndpoints(this RouteGroupBuilder ocpiGroup)
    {
        foreach (var version in Enum.GetValues<OcpiVersion>())
        {
            if (!version.UsesPartyIdInUrls())
                continue;

            var module = ocpiGroup.MapGroup($"{version.ToVersionString()}/chargingprofiles");
            module.AddEndpointFilter(new OcpiContextFilter("chargingprofiles"));
            module.AddEndpointFilter(new OcpiBodySizeLimitFilter(16 * 1024));

            module.MapPost("{correlationId}", HandleChargingProfileResult);
            module.MapPut("{sessionId}", HandleActiveChargingProfileUpdate);
        }

        return ocpiGroup;
    }

    internal static async Task HandleChargingProfileResult(string correlationId, HttpContext httpContext)
    {
        var ctx = httpContext.GetOcpiContext()!; // Safe: OcpiContextFilter runs before handler

        var data = await EndpointHelper
            .DeserializeOrRejectAsync(httpContext, ModuleHandlerFactory.ChargingProfileResult(ctx.NegotiatedVersion))
            .ConfigureAwait(false);
        if (data is null)
            return;

        var callback = httpContext.RequestServices.GetRequiredService<IChargingProfilesCallback>();
        var result = await callback
            .OnChargingProfileResultAsync(ctx, correlationId, data, httpContext.RequestAborted)
            .ConfigureAwait(false);

        await OcpiResponseWriter
            .WriteResultAsync(httpContext, result, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }

    internal static async Task HandleActiveChargingProfileUpdate(string sessionId, HttpContext httpContext)
    {
        var ctx = httpContext.GetOcpiContext()!; // Safe: OcpiContextFilter runs before handler

        var data = await EndpointHelper
            .DeserializeOrRejectAsync(httpContext, ModuleHandlerFactory.ActiveChargingProfile(ctx.NegotiatedVersion))
            .ConfigureAwait(false);
        if (data is null)
            return;

        var callback = httpContext.RequestServices.GetRequiredService<IChargingProfilesCallback>();
        var result = await callback
            .OnActiveChargingProfileUpdateAsync(ctx, sessionId, data, httpContext.RequestAborted)
            .ConfigureAwait(false);

        await OcpiResponseWriter
            .WriteResultAsync(httpContext, result, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }
}
