using DotOcpi.AspNetCore.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace DotOcpi.AspNetCore;

/// <summary>
/// Extension methods for adding DotOcpi ASP.NET Core server integration.
/// </summary>
public static class DotOcpiAspNetCoreExtensions
{
    /// <summary>
    /// Adds the ASP.NET Core server pipeline: OCPI auth filter, exception
    /// middleware, rate limiting, request ID generation, and module endpoint routing.
    /// </summary>
    public static DotOcpiBuilder AddAspNetCoreServer(this DotOcpiBuilder builder)
    {
        builder.Services.AddSingleton<OcpiExceptionMiddleware>();
        builder.Services.AddHostedService<DotOcpiStartupValidator>();

        return builder;
    }
}
