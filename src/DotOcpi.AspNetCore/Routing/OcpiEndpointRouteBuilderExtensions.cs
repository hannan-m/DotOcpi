using DotOcpi.AspNetCore.Filters;
using DotOcpi.AspNetCore.Handlers.Cdrs;
using DotOcpi.AspNetCore.Handlers.ChargingProfiles;
using DotOcpi.AspNetCore.Handlers.Commands;
using DotOcpi.AspNetCore.Handlers.Credentials;
using DotOcpi.AspNetCore.Handlers.Locations;
using DotOcpi.AspNetCore.Handlers.Sessions;
using DotOcpi.AspNetCore.Handlers.Tariffs;
using DotOcpi.AspNetCore.Handlers.Tokens;
using DotOcpi.AspNetCore.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DotOcpi.AspNetCore.Routing;

/// <summary>
/// Extension methods for mapping OCPI endpoints using ASP.NET Core
/// minimal API endpoint routing.
/// </summary>
public static class OcpiEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the OCPI middleware pipeline (request IDs, security headers,
    /// exception handling) and returns the route group for further endpoint
    /// configuration.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="basePath">The base path for all OCPI endpoints (default: "/ocpi").</param>
    /// <param name="rateLimitOptions">Optional rate limit configuration.</param>
    /// <returns>A route group builder for adding OCPI endpoints.</returns>
    public static RouteGroupBuilder MapOcpiEndpoints(
        this WebApplication app,
        string basePath = "/ocpi",
        OcpiRateLimitOptions? rateLimitOptions = null
    )
    {
        // Middleware pipeline (runs on ALL requests, outermost first):
        // 1. Request IDs — ensures X-Request-ID and X-Correlation-ID on every
        //    response, including 404s and unhandled exceptions.
        // 2. Security headers — X-Content-Type-Options, Cache-Control, X-Frame-Options.
        // 3. Exception handler — catches unhandled exceptions, returns OCPI 3000.
        app.UseMiddleware<OcpiRequestIdMiddleware>();
        app.UseMiddleware<OcpiSecurityHeadersMiddleware>();
        app.UseMiddleware<OcpiExceptionMiddleware>();

        var group = app.MapGroup(basePath);

        // Endpoint filter pipeline (runs only on matched OCPI endpoints):
        // 1. Auth — validates token, stores CpoConnection in HttpContext.Items.
        // 2. Rate limit — buckets by CPO (CpoConnection now available).
        // 3. Metrics — records request count and duration.
        // 4. Context — per-module, builds OcpiRequestContext.
        group.AddEndpointFilter<OcpiAuthFilter>();

        if (rateLimitOptions is not null)
        {
            var filter = new OcpiRateLimitFilter(rateLimitOptions);
            group.AddEndpointFilter(filter);
            // Dispose the PartitionedRateLimiter's timers on application shutdown
            app.Lifetime.ApplicationStopping.Register(filter.Dispose);
        }

        group.AddEndpointFilter<OcpiMetricsFilter>();

        return group;
    }

    /// <summary>
    /// Maps all OCPI module endpoints in a single call.
    /// Wires Locations, Sessions, CDRs, Tariffs, Tokens, Commands,
    /// ChargingProfiles, and Credentials endpoints for all supported versions.
    /// Credentials POST (initial registration with Token A) is mapped on a
    /// separate route group outside the auth-filtered pipeline.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="basePath">The base path for all OCPI endpoints (default: "/ocpi").</param>
    /// <param name="rateLimitOptions">Optional rate limit configuration.</param>
    /// <returns>A route group builder for adding additional endpoints.</returns>
    public static RouteGroupBuilder MapAllOcpiEndpoints(
        this WebApplication app,
        string basePath = "/ocpi",
        OcpiRateLimitOptions? rateLimitOptions = null
    )
    {
        var group = app.MapOcpiEndpoints(basePath, rateLimitOptions);

        group.MapLocationsEndpoints();
        group.MapSessionsEndpoints();
        group.MapCdrsEndpoints();
        group.MapTariffsEndpoints();
        group.MapTokensEndpoints();
        group.MapCommandsEndpoints();
        group.MapChargingProfilesEndpoints();
        group.MapCredentialsEndpoints();

        // Registration POST: separate route group, same base path.
        // No OcpiAuthFilter (Token A has no CpoConnection yet).
        // OcpiMetricsFilter for observability. OcpiTokenAAuthFilter validates Token A.
        // ASP.NET Core disambiguates by HTTP method (POST vs PUT/GET/DELETE).
        var registrationGroup = app.MapGroup(basePath);
        registrationGroup.AddEndpointFilter<OcpiMetricsFilter>();
        registrationGroup.MapCredentialsRegistrationEndpoints();

        return group;
    }

    /// <summary>
    /// Maps a module endpoint group that supports both URL patterns:
    /// - 2.0/2.1.1: /{version}/{module}/{object_id}
    /// - 2.2+: /{version}/{module}/{country_code}/{party_id}/{object_id}
    /// </summary>
    /// <param name="group">The parent OCPI route group.</param>
    /// <param name="module">The OCPI module identifier (e.g., "locations").</param>
    /// <returns>A route group builder for adding module endpoints.</returns>
    public static RouteGroupBuilder MapOcpiModule(this RouteGroupBuilder group, string module)
    {
        return group.MapGroup(module);
    }

    /// <summary>
    /// Builds the URL pattern for a versioned object endpoint.
    /// Uses the correct pattern based on whether the version uses party IDs in URLs.
    /// </summary>
    /// <param name="version">The OCPI version.</param>
    /// <param name="withObjectId">Whether to include the object ID segment.</param>
    /// <returns>The URL pattern string.</returns>
    public static string BuildPattern(OcpiVersion version, bool withObjectId = true)
    {
        if (version.UsesPartyIdInUrls())
        {
            return withObjectId ? "{countryCode}/{partyId}/{objectId}" : "{countryCode}/{partyId}";
        }

        return withObjectId ? "{objectId}" : "";
    }
}
