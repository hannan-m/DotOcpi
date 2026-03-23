using DotOcpi.AspNetCore.Filters;
using DotOcpi.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DotOcpi.AspNetCore.Handlers.Commands;

/// <summary>
/// Maps OCPI Commands module callback endpoints for all supported versions.
/// The eMSP receives async command results from CPOs via POST to the response_url.
/// </summary>
[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
    "AOT", "IL2026:RequiresUnreferencedCode",
    Justification = "Endpoint delegates use only string and HttpContext parameters — no reflection-based binding.")]
[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
    "AOT", "IL3050:RequiresDynamicCode",
    Justification = "Endpoint delegates use only string and HttpContext parameters — no runtime code generation needed.")]
public static class CommandsEndpoints
{
    /// <summary>
    /// Maps Commands callback endpoints under the OCPI route group for all versions.
    /// </summary>
    public static RouteGroupBuilder MapCommandsEndpoints(this RouteGroupBuilder ocpiGroup)
    {
        foreach (var version in Enum.GetValues<OcpiVersion>())
        {
            var module = ocpiGroup.MapGroup($"{version.ToVersionString()}/commands");
            module.AddEndpointFilter(new OcpiContextFilter("commands"));
            module.AddEndpointFilter(new OcpiBodySizeLimitFilter(16 * 1024));

            module.MapPost("{correlationId}", HandleCommandCallback);
        }

        return ocpiGroup;
    }

    internal static async Task HandleCommandCallback(string correlationId, HttpContext httpContext)
    {
        var ctx = httpContext.GetOcpiContext()!;

        var data = await EndpointHelper
            .DeserializeOrRejectAsync(httpContext, ModuleHandlerFactory.CommandResult(ctx.NegotiatedVersion))
            .ConfigureAwait(false);
        if (data is null)
            return;

        var callback = httpContext.RequestServices.GetRequiredService<ICommandsCallback>();
        var result = await callback
            .OnCommandResultAsync(ctx, correlationId, data, httpContext.RequestAborted)
            .ConfigureAwait(false);

        await OcpiResponseWriter
            .WriteResultAsync(httpContext, result, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }
}
