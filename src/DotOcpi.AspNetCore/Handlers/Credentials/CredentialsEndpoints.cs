using DotOcpi.AspNetCore.Filters;
using DotOcpi.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DotOcpi.AspNetCore.Handlers.Credentials;

/// <summary>
/// Maps OCPI Credentials module endpoints for all supported versions.
/// Handles the server side of the registration handshake:
/// POST (initial registration), PUT (credential rotation),
/// DELETE (unregistration), and GET (retrieve current credentials).
/// </summary>
public static class CredentialsEndpoints
{
    /// <summary>
    /// Maps Credentials endpoints under the OCPI route group for all versions.
    /// </summary>
    public static RouteGroupBuilder MapCredentialsEndpoints(this RouteGroupBuilder ocpiGroup)
    {
        foreach (var version in Enum.GetValues<OcpiVersion>())
        {
            var module = ocpiGroup.MapGroup($"{version.ToVersionString()}/credentials");
            module.AddEndpointFilter(new OcpiContextFilter("credentials"));

            module.MapPost("", HandleCredentialsPost);
            module.MapPut("", HandleCredentialsPut);
            module.MapDelete("", HandleCredentialsDelete);
            module.MapGet("", HandleCredentialsGet);
        }

        return ocpiGroup;
    }

    internal static async Task HandleCredentialsPost(HttpContext httpContext)
    {
        var ctx = httpContext.GetOcpiContext()!;

        var data = await EndpointHelper
            .DeserializeOrRejectAsync(httpContext, ModuleHandlerFactory.Credentials(ctx.NegotiatedVersion))
            .ConfigureAwait(false);
        if (data is null)
            return;

        var handler = httpContext.RequestServices.GetRequiredService<ICredentialsHandler>();
        var result = await handler.OnCredentialsPostAsync(ctx, data, httpContext.RequestAborted).ConfigureAwait(false);

        await OcpiResponseWriter
            .WriteResultObjectAsync(httpContext, result, ctx.NegotiatedVersion, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }

    internal static async Task HandleCredentialsPut(HttpContext httpContext)
    {
        var ctx = httpContext.GetOcpiContext()!;

        var data = await EndpointHelper
            .DeserializeOrRejectAsync(httpContext, ModuleHandlerFactory.Credentials(ctx.NegotiatedVersion))
            .ConfigureAwait(false);
        if (data is null)
            return;

        var handler = httpContext.RequestServices.GetRequiredService<ICredentialsHandler>();
        var result = await handler.OnCredentialsPutAsync(ctx, data, httpContext.RequestAborted).ConfigureAwait(false);

        await OcpiResponseWriter
            .WriteResultObjectAsync(httpContext, result, ctx.NegotiatedVersion, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }

    internal static async Task HandleCredentialsDelete(HttpContext httpContext)
    {
        var ctx = httpContext.GetOcpiContext()!;

        var handler = httpContext.RequestServices.GetRequiredService<ICredentialsHandler>();
        var result = await handler.OnCredentialsDeleteAsync(ctx, httpContext.RequestAborted).ConfigureAwait(false);

        await OcpiResponseWriter
            .WriteResultAsync(httpContext, result, ctx.NegotiatedVersion, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }

    internal static async Task HandleCredentialsGet(HttpContext httpContext)
    {
        var ctx = httpContext.GetOcpiContext()!;

        var handler = httpContext.RequestServices.GetRequiredService<ICredentialsHandler>();
        var result = await handler.GetCredentialsAsync(ctx, httpContext.RequestAborted).ConfigureAwait(false);

        await OcpiResponseWriter
            .WriteResultObjectAsync(httpContext, result, ctx.NegotiatedVersion, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }
}
