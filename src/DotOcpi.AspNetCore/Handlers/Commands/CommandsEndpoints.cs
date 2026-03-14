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

            module.MapPost("{correlationId}", HandleCommandCallback);
        }

        return ocpiGroup;
    }

    internal static async Task HandleCommandCallback(HttpContext httpContext)
    {
        var ctx = httpContext.GetOcpiContext()!;
        var correlationId = (string)httpContext.GetRouteValue("correlationId")!;

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
            .WriteResultAsync(httpContext, result, ctx.NegotiatedVersion, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }
}
