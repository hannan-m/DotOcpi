using DotOcpi.AspNetCore.Filters;
using DotOcpi.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DotOcpi.AspNetCore.Handlers.Locations;

/// <summary>
/// Maps OCPI Locations module endpoints for all supported versions.
/// Registers both URL patterns: flat (2.0/2.1.1) and party-prefixed (2.2+).
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
public static class LocationsEndpoints
{
    /// <summary>
    /// Maps Locations receiver endpoints under the OCPI route group for all versions.
    /// </summary>
    public static RouteGroupBuilder MapLocationsEndpoints(this RouteGroupBuilder ocpiGroup)
    {
        foreach (var version in Enum.GetValues<OcpiVersion>())
        {
            var module = ocpiGroup.MapGroup($"{version.ToVersionString()}/locations");
            module.AddEndpointFilter(new OcpiContextFilter("locations"));
            module.AddEndpointFilter(new OcpiBodySizeLimitFilter(256 * 1024));
            RegisterRoutes(module, version.UsesPartyIdInUrls());
        }

        return ocpiGroup;
    }

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
    private static void RegisterRoutes(RouteGroupBuilder group, bool usesPartyId)
    {
        var prefix = usesPartyId ? "{countryCode}/{partyId}/" : "";

        group.MapPut($"{prefix}{{locationId}}", HandleLocationPut);
        group.MapPatch($"{prefix}{{locationId}}", HandleLocationPatch);
        group.MapGet($"{prefix}{{locationId}}", HandleLocationGet);

        group.MapPut($"{prefix}{{locationId}}/{{evseUid}}", HandleEvsePut);
        group.MapPatch($"{prefix}{{locationId}}/{{evseUid}}", HandleEvsePatch);

        group.MapPut($"{prefix}{{locationId}}/{{evseUid}}/{{connectorId}}", HandleConnectorPut);
        group.MapPatch($"{prefix}{{locationId}}/{{evseUid}}/{{connectorId}}", HandleConnectorPatch);
    }

    internal static async Task HandleLocationPut(string locationId, HttpContext httpContext)
    {
        var ctx = httpContext.GetOcpiContext()!;

        var data = await EndpointHelper
            .DeserializeOrRejectAsync(httpContext, ModuleHandlerFactory.Location(ctx.NegotiatedVersion))
            .ConfigureAwait(false);
        if (data is null)
            return;

        var receiver = httpContext.RequestServices.GetRequiredService<ILocationsReceiver>();
        var result = await receiver
            .OnLocationPutAsync(ctx, locationId, data, httpContext.RequestAborted)
            .ConfigureAwait(false);

        await OcpiResponseWriter
            .WriteResultAsync(httpContext, result, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }

    internal static async Task HandleLocationPatch(string locationId, HttpContext httpContext)
    {
        var ctx = httpContext.GetOcpiContext()!;

        var patch = await EndpointHelper.ReadPatchAsync(httpContext).ConfigureAwait(false);
        if (patch is null)
            return;

        var receiver = httpContext.RequestServices.GetRequiredService<ILocationsReceiver>();
        var result = await receiver
            .OnLocationPatchAsync(ctx, locationId, patch.Value, httpContext.RequestAborted)
            .ConfigureAwait(false);

        await OcpiResponseWriter
            .WriteResultAsync(httpContext, result, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }

    internal static async Task HandleLocationGet(string locationId, HttpContext httpContext)
    {
        var ctx = httpContext.GetOcpiContext()!;

        var receiver = httpContext.RequestServices.GetRequiredService<ILocationsReceiver>();
        var result = await receiver.GetLocationAsync(ctx, locationId, httpContext.RequestAborted).ConfigureAwait(false);

        await OcpiResponseWriter
            .WriteResultObjectAsync(httpContext, result, ctx.NegotiatedVersion, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }

    internal static async Task HandleEvsePut(string locationId, string evseUid, HttpContext httpContext)
    {
        var ctx = httpContext.GetOcpiContext()!;

        var data = await EndpointHelper
            .DeserializeOrRejectAsync(httpContext, ModuleHandlerFactory.Evse(ctx.NegotiatedVersion))
            .ConfigureAwait(false);
        if (data is null)
            return;

        var receiver = httpContext.RequestServices.GetRequiredService<ILocationsReceiver>();
        var result = await receiver
            .OnEvsePutAsync(ctx, locationId, evseUid, data, httpContext.RequestAborted)
            .ConfigureAwait(false);

        await OcpiResponseWriter
            .WriteResultAsync(httpContext, result, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }

    internal static async Task HandleEvsePatch(string locationId, string evseUid, HttpContext httpContext)
    {
        var ctx = httpContext.GetOcpiContext()!;

        var patch = await EndpointHelper.ReadPatchAsync(httpContext).ConfigureAwait(false);
        if (patch is null)
            return;

        var receiver = httpContext.RequestServices.GetRequiredService<ILocationsReceiver>();
        var result = await receiver
            .OnEvsePatchAsync(ctx, locationId, evseUid, patch.Value, httpContext.RequestAborted)
            .ConfigureAwait(false);

        await OcpiResponseWriter
            .WriteResultAsync(httpContext, result, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }

    internal static async Task HandleConnectorPut(
        string locationId,
        string evseUid,
        string connectorId,
        HttpContext httpContext
    )
    {
        var ctx = httpContext.GetOcpiContext()!;

        var data = await EndpointHelper
            .DeserializeOrRejectAsync(httpContext, ModuleHandlerFactory.Connector(ctx.NegotiatedVersion))
            .ConfigureAwait(false);
        if (data is null)
            return;

        var receiver = httpContext.RequestServices.GetRequiredService<ILocationsReceiver>();
        var result = await receiver
            .OnConnectorPutAsync(ctx, locationId, evseUid, connectorId, data, httpContext.RequestAborted)
            .ConfigureAwait(false);

        await OcpiResponseWriter
            .WriteResultAsync(httpContext, result, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }

    internal static async Task HandleConnectorPatch(
        string locationId,
        string evseUid,
        string connectorId,
        HttpContext httpContext
    )
    {
        var ctx = httpContext.GetOcpiContext()!;

        var patch = await EndpointHelper.ReadPatchAsync(httpContext).ConfigureAwait(false);
        if (patch is null)
            return;

        var receiver = httpContext.RequestServices.GetRequiredService<ILocationsReceiver>();
        var result = await receiver
            .OnConnectorPatchAsync(ctx, locationId, evseUid, connectorId, patch.Value, httpContext.RequestAborted)
            .ConfigureAwait(false);

        await OcpiResponseWriter
            .WriteResultAsync(httpContext, result, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }
}
