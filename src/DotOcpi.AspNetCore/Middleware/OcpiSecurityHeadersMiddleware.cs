using Microsoft.AspNetCore.Http;

namespace DotOcpi.AspNetCore.Middleware;

/// <summary>
/// Middleware that sets required OCPI security response headers on every response:
/// X-Content-Type-Options, Cache-Control, and X-Frame-Options.
/// </summary>
public sealed class OcpiSecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public OcpiSecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        httpContext.Response.OnStarting(
            static state =>
            {
                var ctx = (HttpContext)state;
                var headers = ctx.Response.Headers;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["Cache-Control"] = "no-store";
                headers["X-Frame-Options"] = "DENY";
                return Task.CompletedTask;
            },
            httpContext
        );

        await _next(httpContext).ConfigureAwait(false);
    }
}
