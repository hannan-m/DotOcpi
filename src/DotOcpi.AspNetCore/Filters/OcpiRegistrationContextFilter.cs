using DotOcpi.Security;
using Microsoft.AspNetCore.Http;

namespace DotOcpi.AspNetCore.Filters;

/// <summary>
/// Endpoint filter that assembles an <see cref="OcpiRegistrationContext"/> from
/// the Token A validation result and request ID pipeline, then stores it on
/// <see cref="HttpContext"/> for the credentials POST handler.
/// </summary>
internal sealed class OcpiRegistrationContextFilter : IEndpointFilter
{
    private readonly OcpiVersion _version;

    public OcpiRegistrationContextFilter(OcpiVersion version)
    {
        _version = version;
    }

    /// <inheritdoc />
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var tokenEntry = httpContext.Items.TryGetValue(typeof(TokenEntry), out var value) ? value as TokenEntry : null;

        if (tokenEntry is null)
        {
            return ValueTask.FromResult<object?>(
                OcpiResponseWriter.ErrorResult(
                    StatusCodes.Status500InternalServerError,
                    3000,
                    "Server configuration error."
                )
            );
        }

        var registrationContext = new OcpiRegistrationContext
        {
            RequestId = httpContext.GetRequestId() ?? "",
            CorrelationId = httpContext.GetCorrelationId() ?? "",
            Version = _version,
            TokenAEntry = tokenEntry,
        };

        httpContext.SetRegistrationContext(registrationContext);
        return next(context);
    }
}
