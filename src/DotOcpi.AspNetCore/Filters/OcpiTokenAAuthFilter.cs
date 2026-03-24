using DotOcpi.Logging;
using DotOcpi.Observability;
using DotOcpi.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DotOcpi.AspNetCore.Filters;

/// <summary>
/// Endpoint filter that validates OCPI Token A authorization headers for
/// initial CPO registration (POST /credentials). Rejects Token B/C —
/// registration must use the pre-shared Token A.
/// Does not require or look up a <see cref="Registry.CpoConnection"/>.
/// </summary>
public sealed class OcpiTokenAAuthFilter : IEndpointFilter
{
    private readonly OcpiTokenValidator _tokenValidator;
    private readonly OcpiMetrics _metrics;
    private readonly ILogger<OcpiTokenAAuthFilter> _logger;

    public OcpiTokenAAuthFilter(
        OcpiTokenValidator tokenValidator,
        OcpiMetrics metrics,
        ILogger<OcpiTokenAAuthFilter> logger
    )
    {
        _tokenValidator = tokenValidator;
        _metrics = metrics;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var requestId = httpContext.Request.Headers["X-Request-ID"].ToString();
        var authHeader = httpContext.Request.Headers.Authorization.ToString();

        if (!AuthorizationHeaderParser.TryParse(authHeader, out var rawToken))
        {
            _metrics.RecordAuthFailure("missing_header");
            _logger.AuthMissingToken(requestId);
            return CreateUnauthorizedResponse("Missing or malformed Authorization header.");
        }

        var validationResult = await _tokenValidator
            .ValidateAsync(rawToken, httpContext.RequestAborted)
            .ConfigureAwait(false);

        if (!validationResult.IsValid)
        {
            _metrics.RecordAuthFailure("invalid_token");
            _logger.AuthInvalidToken(requestId);
            return CreateUnauthorizedResponse("Invalid or unrecognized token.");
        }

        if (validationResult.Entry!.Purpose != TokenPurpose.TokenA)
        {
            _metrics.RecordAuthFailure("wrong_token_purpose");
            _logger.AuthWrongTokenPurpose(
                requestId,
                nameof(TokenPurpose.TokenA),
                validationResult.Entry.Purpose.ToString()
            );
            return CreateUnauthorizedResponse("Initial registration requires Token A.");
        }

        httpContext.Items[typeof(TokenEntry)] = validationResult.Entry;

        return await next(context).ConfigureAwait(false);
    }

    private static IResult CreateUnauthorizedResponse(string message) =>
        OcpiResponseWriter.ErrorResult(StatusCodes.Status401Unauthorized, 2002, message);
}
