using System.Text.Json;
using DotOcpi.Registry;
using DotOcpi.Security;
using Microsoft.AspNetCore.Http;

namespace DotOcpi.AspNetCore.Filters;

/// <summary>
/// Endpoint filter that validates OCPI Token authorization headers.
/// Rejects requests with missing or invalid tokens with HTTP 401
/// and OCPI status code 2002 (not enough information).
/// </summary>
public sealed class OcpiAuthFilter : IEndpointFilter
{
    private readonly OcpiTokenValidator _tokenValidator;
    private readonly ICpoRegistry _registry;

    /// <summary>
    /// Creates a new OCPI auth filter.
    /// </summary>
    public OcpiAuthFilter(OcpiTokenValidator tokenValidator, ICpoRegistry registry)
    {
        _tokenValidator = tokenValidator;
        _registry = registry;
    }

    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var authHeader = httpContext.Request.Headers.Authorization.ToString();

        if (!AuthorizationHeaderParser.TryParse(authHeader, out var rawToken))
        {
            return CreateUnauthorizedResponse(httpContext, "Missing or malformed Authorization header.");
        }

        var validationResult = await _tokenValidator
            .ValidateAsync(rawToken, httpContext.RequestAborted)
            .ConfigureAwait(false);

        if (!validationResult.IsValid)
        {
            return CreateUnauthorizedResponse(httpContext, "Invalid or unrecognized token.");
        }

        // Look up the CPO connection by the token hash
        var tokenHash = TokenHasher.Hash(rawToken);
        var connection = _registry.FindByTokenHash(tokenHash);
        if (connection is null)
        {
            return CreateUnauthorizedResponse(httpContext, "No CPO connection associated with this token.");
        }

        // Store the connection in HttpContext for downstream handlers
        httpContext.Items[typeof(CpoConnection)] = connection;

        return await next(context).ConfigureAwait(false);
    }

    private static IResult CreateUnauthorizedResponse(HttpContext httpContext, string message)
    {
        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;

        return Results.Json(
            new
            {
                status_code = 2002,
                status_message = message,
                timestamp = DateTimeOffset.UtcNow,
            },
            statusCode: StatusCodes.Status401Unauthorized,
            contentType: "application/json"
        );
    }
}
