using DotOcpi.Observability;
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
    private readonly OcpiMetrics _metrics;

    public OcpiAuthFilter(OcpiTokenValidator tokenValidator, ICpoRegistry registry, OcpiMetrics metrics)
    {
        _tokenValidator = tokenValidator;
        _registry = registry;
        _metrics = metrics;
    }

    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var authHeader = httpContext.Request.Headers.Authorization.ToString();

        if (!AuthorizationHeaderParser.TryParse(authHeader, out var rawToken))
        {
            _metrics.RecordAuthFailure("missing_header");
            return CreateUnauthorizedResponse("Missing or malformed Authorization header.");
        }

        var validationResult = await _tokenValidator
            .ValidateAsync(rawToken, httpContext.RequestAborted)
            .ConfigureAwait(false);

        if (!validationResult.IsValid)
        {
            _metrics.RecordAuthFailure("invalid_token");
            return CreateUnauthorizedResponse("Invalid or unrecognized token.");
        }

        // Use the hash already computed by the validator — no need to hash again.
        var connection = _registry.FindByTokenHash(validationResult.Entry!.TokenHash);
        if (connection is null)
        {
            _metrics.RecordAuthFailure("no_connection");
            return CreateUnauthorizedResponse("No CPO connection associated with this token.");
        }

        // Store the connection in HttpContext for downstream filters and handlers
        httpContext.Items[typeof(CpoConnection)] = connection;

        return await next(context).ConfigureAwait(false);
    }

    private static IResult CreateUnauthorizedResponse(string message) =>
        OcpiResponseWriter.ErrorResult(StatusCodes.Status401Unauthorized, 2002, message);
}
