using DotOcpi.Registry;
using Microsoft.AspNetCore.Http;

namespace DotOcpi.AspNetCore.Filters;

/// <summary>
/// Endpoint filter that assembles an <see cref="OcpiRequestContext"/> from the
/// authentication and request ID pipeline results, then stores it on
/// <see cref="HttpContext"/> for downstream handlers.
/// </summary>
internal sealed class OcpiContextFilter : IEndpointFilter
{
    private readonly string _moduleId;

    public OcpiContextFilter(string moduleId)
    {
        _moduleId = moduleId;
    }

    /// <inheritdoc />
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var connection = httpContext.Items[typeof(CpoConnection)] as CpoConnection;

        if (connection is null)
        {
            // Auth filter should have rejected already; this is a pipeline misconfiguration
            return ValueTask.FromResult<object?>(
                OcpiResponseWriter.ErrorResult(
                    StatusCodes.Status500InternalServerError,
                    3000,
                    "Server configuration error."
                )
            );
        }

        var ocpiContext = new OcpiRequestContext
        {
            Connection = connection,
            RequestId = httpContext.GetRequestId() ?? "",
            CorrelationId = httpContext.GetCorrelationId() ?? "",
            CpoId = $"{connection.CpoCountryCode}_{connection.CpoPartyId}",
            CpoIdentity = new PartyIdentity(connection.CpoCountryCode, connection.CpoPartyId),
            EmspIdentity = new PartyIdentity(connection.EmspCountryCode, connection.EmspPartyId),
            NegotiatedVersion = connection.Version,
            ModuleId = _moduleId,
        };

        httpContext.SetOcpiContext(ocpiContext);
        return next(context);
    }
}
